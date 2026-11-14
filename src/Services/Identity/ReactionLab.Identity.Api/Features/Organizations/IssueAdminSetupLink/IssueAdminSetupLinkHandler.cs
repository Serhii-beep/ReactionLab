using ReactionLab.Identity.Api.Features.AccountSetups;
using ReactionLab.Identity.Api.Features.Members;
using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Api.Features.PlatformRoles;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations.IssueAdminSetupLink;

internal sealed class IssueAdminSetupLinkHandler(
    RecentSignInCheck recentSignInCheck,
    PlatformAdminTransaction adminTransaction,
    AppDbContext context,
    MailedAccountSetups mailedSetups,
    TimeProvider timeProvider,
    ILogger<IssueAdminSetupLinkHandler> logger)
    : ICommandHandler<IssueAdminSetupLinkCommand, AccountSetupLinkResponse>
{
    public async ValueTask<Result<AccountSetupLinkResponse>> HandleAsync(
        IssueAdminSetupLinkCommand command,
        CancellationToken cancellationToken)
    {
        var state = CheckWaiting(await context.FindAdminSetupStateAsync(
            command.OrganizationId,
            command.AccountId,
            cancellationToken));

        if (state.IsFailure)
        {
            return Result.Failure<AccountSetupLinkResponse>(state.Error);
        }

        var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
            command.AdminId,
            command.AdminSessionId,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not issue a setup link for account {AccountId}: {ErrorCode}.",
                command.AdminSessionId,
                command.AccountId,
                proof.Error.Code);

            return Result.Failure<AccountSetupLinkResponse>(proof.Error);
        }

        var now = timeProvider.GetDatabaseUtcNow();
        var mailed = state.Value.HasEmail;
        (string? Token, byte[]? TokenHash) setupLink = mailed ? (null, null) : OneTimeLinkTokens.Issue();
        var setupExpiresAt = now + AccountSetup.Lifetime;

        var issued = await adminTransaction.RunAsync(
            command.AdminId,
            command.AdminSessionId,
            () => IssueAsync(command, setupLink.TokenHash, mailed, now, setupExpiresAt));

        if (issued.IsFailure)
        {
            return Result.Failure<AccountSetupLinkResponse>(issued.Error);
        }

        logger.LogInformation(
            "Admin {AdminId} issued a setup link for account {AccountId} in session {SessionId}, mailed: {Mailed}.",
            command.AdminId,
            command.AccountId,
            command.AdminSessionId,
            mailed);

        return Result.Success(new AccountSetupLinkResponse(
            command.AccountId,
            issued.Value.Username,
            setupLink.Token,
            setupExpiresAt,
            SetupLinkMailed: mailed));
    }

    private async Task<Result<AdminSetupState>> IssueAsync(
        IssueAdminSetupLinkCommand command,
        byte[]? setupTokenHash,
        bool mailed,
        DateTimeOffset now,
        DateTimeOffset setupExpiresAt)
    {
        await context.LockOrganizationsAsync([command.OrganizationId]);
        await context.LockSchoolAccountAsync(command.OrganizationId, command.AccountId);

        var state = CheckWaiting(await context.FindAdminSetupStateAsync(
            command.OrganizationId,
            command.AccountId,
            CancellationToken.None));

        if (state.IsFailure)
        {
            return state;
        }

        if (mailed && !await mailedSetups.RequestAsync(
            command.OrganizationId,
            command.AccountId,
            command.AdminId,
            command.AdminSessionId))
        {
            return Result.Failure<AdminSetupState>(MemberErrors.SetupLinkLimitReached);
        }

        if (!mailed && await context.IssueAccountSetupAsync(
            command.OrganizationId,
            command.AccountId,
            setupTokenHash!,
            now,
            setupExpiresAt,
            command.AdminId,
            command.AdminSessionId) != 1)
        {
            throw new InvalidOperationException("The setup link was not stored.");
        }

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            mailed ? OrganizationChangeNames.SetupLinkMailed : OrganizationChangeNames.SetupLinkIssued,
            command.AccountId,
            role: null,
            command.AdminId,
            command.AdminSessionId);

        return state;
    }

    private static Result<AdminSetupState> CheckWaiting(AdminSetupState? state) => state switch
    {
        null => OrganizationErrors.AdminNotFound,
        { IsDisabled: true } => OrganizationErrors.AdminDisabled,
        { OrganizationHasAdminWhoCanSignIn: true } => OrganizationErrors.AlreadySetUp,
        _ => state
    };
}

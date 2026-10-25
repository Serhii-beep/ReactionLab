using ReactionLab.Identity.Api.Features.AccountSetups;
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
        var (setupToken, setupTokenHash) = AccountSetupTokens.Issue();
        var setupExpiresAt = now + AccountSetup.Lifetime;

        var issued = await adminTransaction.RunAsync(
            command.AdminId,
            command.AdminSessionId,
            () => IssueAsync(command, setupTokenHash, now, setupExpiresAt));

        if (issued.IsFailure)
        {
            return Result.Failure<AccountSetupLinkResponse>(issued.Error);
        }

        logger.LogInformation(
            "Admin {AdminId} issued a setup link for account {AccountId} in session {SessionId}.",
            command.AdminId,
            command.AccountId,
            command.AdminSessionId);

        return Result.Success(new AccountSetupLinkResponse(
            command.AccountId,
            issued.Value.Username,
            setupToken,
            setupExpiresAt));
    }

    private async Task<Result<AdminSetupState>> IssueAsync(
        IssueAdminSetupLinkCommand command,
        byte[] setupTokenHash,
        DateTimeOffset now,
        DateTimeOffset setupExpiresAt)
    {
        await context.LockSchoolAccountAsync(command.OrganizationId, command.AccountId);

        var state = CheckWaiting(await context.FindAdminSetupStateAsync(
            command.OrganizationId,
            command.AccountId,
            CancellationToken.None));

        if (state.IsFailure)
        {
            return state;
        }

        var issued = await context.IssueAccountSetupAsync(
            command.OrganizationId,
            command.AccountId,
            setupTokenHash,
            now,
            setupExpiresAt,
            command.AdminId,
            command.AdminSessionId);

        if (issued != 1)
        {
            throw new InvalidOperationException("The setup link was not stored.");
        }

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.SetupLinkIssued,
            command.AccountId,
            role: null,
            command.AdminId,
            command.AdminSessionId);

        return state;
    }

    private static Result<AdminSetupState> CheckWaiting(AdminSetupState? state) => state switch
    {
        null => OrganizationErrors.AdminNotFound,
        { HasPassword: true } or { OrganizationHasAdminWithPassword: true } => OrganizationErrors.AlreadySetUp,
        _ => state
    };
}

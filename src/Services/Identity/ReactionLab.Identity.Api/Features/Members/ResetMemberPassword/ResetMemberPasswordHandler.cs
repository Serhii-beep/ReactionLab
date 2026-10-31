using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.ResetMemberPassword;

internal sealed class ResetMemberPasswordHandler(
    SchoolAdminTransaction adminTransaction,
    MemberCredentials memberCredentials,
    UserManager<User> userManager,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<ResetMemberPasswordHandler> logger)
    : ICommandHandler<ResetMemberPasswordCommand, MemberCredentialResponse>
{
    public async ValueTask<Result<MemberCredentialResponse>> HandleAsync(
        ResetMemberPasswordCommand command,
        CancellationToken cancellationToken)
    {
        var access = await adminTransaction.CheckAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.StudentPasswordReset,
            cancellationToken);

        if (access.IsFailure)
        {
            return Result.Failure<MemberCredentialResponse>(access.Error);
        }

        var kind = MemberCredentials.ReadKind(command.Credential);

        if (kind.IsFailure)
        {
            return Result.Failure<MemberCredentialResponse>(kind.Error);
        }

        var target = await CheckTargetAsync(command, access.Value, cancellationToken);

        if (target.IsFailure)
        {
            return Result.Failure<MemberCredentialResponse>(target.Error);
        }

        if (await userManager.FindByIdAsync(command.AccountId.ToString()) is not { } account)
        {
            return Result.Failure<MemberCredentialResponse>(MemberErrors.NotFound);
        }

        var credential = await memberCredentials.PrepareAsync(account, kind.Value, cancellationToken);
        var reset = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.StudentPasswordReset,
            [],
            command.AccountId,
            lockedAccess => ResetAsync(command, lockedAccess, credential));

        if (reset.IsFailure)
        {
            return Result.Failure<MemberCredentialResponse>(reset.Error);
        }

        logger.LogInformation(
            "Account {ActorId} gave account {AccountId} a new {Credential} in session {SessionId}.",
            command.ActorId,
            command.AccountId,
            credential.Kind,
            command.ActorSessionId);

        return Result.Success(credential.ToResponse());
    }

    private async Task<Result<bool>> ResetAsync(
        ResetMemberPasswordCommand command,
        OrganizationAccess access,
        MemberCredential credential)
    {
        var target = await CheckTargetAsync(command, access, CancellationToken.None);

        if (target.IsFailure)
        {
            return target.Error;
        }

        if (!await memberCredentials.ApplyAsync(
            credential,
            command.AccountId,
            command.OrganizationId,
            command.ActorId,
            command.ActorSessionId))
        {
            return MemberErrors.NotFound;
        }

        await MemberSessions.EndAllAsync(context, command.AccountId);

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            credential.Kind == MemberCredentialKinds.SetupLink
                ? OrganizationChangeNames.SetupLinkIssued
                : OrganizationChangeNames.PasswordReset,
            command.AccountId,
            role: null,
            command.ActorId,
            command.ActorSessionId);

        return true;
    }

    private async Task<Result<SchoolMember>> CheckTargetAsync(
        ResetMemberPasswordCommand command,
        OrganizationAccess access,
        CancellationToken cancellationToken)
    {
        var member = MemberTargets.Managed(
            await context.FindMemberAsync(
                command.OrganizationId,
                access.DistrictId,
                command.AccountId,
                timeProvider.GetUtcNow(),
                cancellationToken),
            command.OrganizationId);

        if (member.IsFailure || access.IsAdmin)
        {
            return member;
        }

        return member.Value.Roles is [SchoolRoleNames.Student] ? member : MemberErrors.ResetNotAllowed;
    }
}

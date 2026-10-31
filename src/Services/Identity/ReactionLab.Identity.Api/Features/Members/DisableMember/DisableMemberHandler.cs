using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.DisableMember;

internal sealed class DisableMemberHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<DisableMemberHandler> logger) : ICommandHandler<DisableMemberCommand>
{
    public async ValueTask<Result> HandleAsync(DisableMemberCommand command, CancellationToken cancellationToken)
    {
        var access = await adminTransaction.CheckAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            cancellationToken);

        if (access.IsFailure)
        {
            return access;
        }

        if (await CheckAsync(command, access.Value, cancellationToken) is { } refusal)
        {
            return Result.Failure(refusal);
        }

        var disabled = await adminTransaction.RunLockingMemberAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            command.AccountId,
            SchoolAdminRules.Admin,
            lockedAccess => DisableAsync(command, lockedAccess),
            cancellationToken);

        if (disabled.IsFailure)
        {
            return disabled;
        }

        if (disabled.Value)
        {
            logger.LogInformation(
                "Account {ActorId} disabled account {AccountId} in session {SessionId}.",
                command.ActorId,
                command.AccountId,
                command.ActorSessionId);
        }

        return Result.Success();
    }

    private async Task<Result<bool>> DisableAsync(DisableMemberCommand command, OrganizationAccess access)
    {
        if (await CheckAsync(command, access, CancellationToken.None) is { } refusal)
        {
            return refusal;
        }

        var disabled = await context.DisableMemberAsync(command.AccountId, command.OrganizationId) == 1;
        var endedSessions = await MemberSessions.EndAllAsync(context, command.AccountId);

        if (disabled)
        {
            await context.RecordAccountChangeAsync(
                command.AccountId,
                OrganizationChangeNames.AccountDisabled,
                command.ActorId,
                command.ActorSessionId);
        }
        else if (endedSessions > 0)
        {
            await context.RecordOrganizationChangeAsync(
                command.OrganizationId,
                OrganizationChangeNames.SessionsEnded,
                command.AccountId,
                role: null,
                command.ActorId,
                command.ActorSessionId);
        }

        return disabled;
    }

    private async Task<Error?> CheckAsync(
        DisableMemberCommand command,
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

        if (member.IsFailure)
        {
            return member.Error;
        }

        return await context.IsLastEnabledAdminAsync(command.AccountId, null, cancellationToken)
            ? MemberErrors.LastAdmin
            : null;
    }
}

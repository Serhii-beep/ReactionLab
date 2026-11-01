using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.Messaging.Outbox;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.DeleteMember;

internal sealed class DeleteMemberHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    OutboxWriter outbox,
    TimeProvider timeProvider,
    ILogger<DeleteMemberHandler> logger) : ICommandHandler<DeleteMemberCommand>
{
    public async ValueTask<Result> HandleAsync(DeleteMemberCommand command, CancellationToken cancellationToken)
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

        var deleted = await adminTransaction.RunLockingMemberAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            command.AccountId,
            SchoolAdminRules.Admin,
            lockedAccess => DeleteAsync(command, lockedAccess),
            cancellationToken);

        if (deleted.IsFailure)
        {
            return deleted;
        }

        if (deleted.Value)
        {
            logger.LogInformation(
                "Account {ActorId} deleted account {AccountId} of {OrganizationId} in session {SessionId}.",
                command.ActorId,
                command.AccountId,
                command.OrganizationId,
                command.ActorSessionId);
        }

        return Result.Success();
    }

    private async Task<Result<bool>> DeleteAsync(DeleteMemberCommand command, OrganizationAccess access)
    {
        if (await CheckAsync(command, access, CancellationToken.None) is { } refusal)
        {
            var alreadyDeleted = refusal == MemberErrors.NotFound
                && !await context.AccountExistsAsync(command.AccountId, CancellationToken.None);

            return alreadyDeleted ? false : refusal;
        }

        await MemberSessions.EndAllAsync(context, command.AccountId);

        await context.RecordAccountChangeAsync(
            command.AccountId,
            OrganizationChangeNames.AccountDeleted,
            command.ActorId,
            command.ActorSessionId);

        if (await context.DeleteMemberAsync(command.AccountId, command.OrganizationId) is not { } deletedVersion)
        {
            return MemberErrors.NotFound;
        }

        await outbox.InsertAsync(
            context,
            new UserErasureRequested(command.AccountId, deletedVersion + 1, command.OrganizationId));
        await outbox.InsertTombstoneAsync(context, IdentityTopics.Profiles, command.AccountId);

        return true;
    }

    private async Task<Error?> CheckAsync(
        DeleteMemberCommand command,
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

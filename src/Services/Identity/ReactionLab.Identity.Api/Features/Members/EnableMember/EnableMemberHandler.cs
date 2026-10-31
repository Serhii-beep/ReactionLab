using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.EnableMember;

internal sealed class EnableMemberHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<EnableMemberHandler> logger) : ICommandHandler<EnableMemberCommand>
{
    public async ValueTask<Result> HandleAsync(EnableMemberCommand command, CancellationToken cancellationToken)
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

        var disabled = await CheckDisabledAsync(command, access.Value, cancellationToken);

        if (disabled.IsFailure || !disabled.Value)
        {
            return disabled;
        }

        var enabled = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            [],
            command.AccountId,
            lockedAccess => EnableAsync(command, lockedAccess));

        if (enabled.IsFailure)
        {
            return enabled;
        }

        if (enabled.Value)
        {
            logger.LogInformation(
                "Account {ActorId} enabled account {AccountId} in session {SessionId}.",
                command.ActorId,
                command.AccountId,
                command.ActorSessionId);
        }

        return Result.Success();
    }

    private async Task<Result<bool>> EnableAsync(EnableMemberCommand command, OrganizationAccess access)
    {
        var disabled = await CheckDisabledAsync(command, access, CancellationToken.None);

        if (disabled.IsFailure || !disabled.Value)
        {
            return disabled;
        }

        if (await context.EnableMemberAsync(command.AccountId, command.OrganizationId) != 1)
        {
            return false;
        }

        await context.RecordAccountChangeAsync(
            command.AccountId,
            OrganizationChangeNames.AccountEnabled,
            command.ActorId,
            command.ActorSessionId);

        return true;
    }

    private async Task<Result<bool>> CheckDisabledAsync(
        EnableMemberCommand command,
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

        return member.IsFailure ? member.Error : member.Value.DisabledAt is not null;
    }
}

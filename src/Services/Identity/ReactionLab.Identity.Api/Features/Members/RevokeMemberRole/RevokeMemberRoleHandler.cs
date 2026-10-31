using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.RevokeMemberRole;

internal sealed class RevokeMemberRoleHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<RevokeMemberRoleHandler> logger) : ICommandHandler<RevokeMemberRoleCommand>
{
    public async ValueTask<Result> HandleAsync(RevokeMemberRoleCommand command, CancellationToken cancellationToken)
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

        if (!SchoolRoleNames.All.Contains(command.Role))
        {
            return Result.Failure(MemberErrors.RoleUnknown);
        }

        var held = await CheckRevocationAsync(command, access.Value, cancellationToken);

        if (held.IsFailure || !held.Value)
        {
            return held;
        }

        var revoked = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            [],
            command.AccountId,
            lockedAccess => RevokeAsync(command, lockedAccess));

        if (revoked.IsFailure)
        {
            return revoked;
        }

        if (revoked.Value)
        {
            logger.LogInformation(
                "Account {ActorId} revoked {Role} at {OrganizationId} from account {AccountId} in session {SessionId}.",
                command.ActorId,
                command.Role,
                command.OrganizationId,
                command.AccountId,
                command.ActorSessionId);
        }

        return Result.Success();
    }

    private async Task<Result<bool>> RevokeAsync(RevokeMemberRoleCommand command, OrganizationAccess access)
    {
        var held = await CheckRevocationAsync(command, access, CancellationToken.None);

        if (held.IsFailure || !held.Value)
        {
            return held;
        }

        if (await context.RevokeMemberRoleAsync(command.AccountId, command.OrganizationId, command.Role) != 1)
        {
            return MemberErrors.LastAdmin;
        }

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.RoleRevoked,
            command.AccountId,
            command.Role,
            command.ActorId,
            command.ActorSessionId);

        return true;
    }

    private async Task<Result<bool>> CheckRevocationAsync(
        RevokeMemberRoleCommand command,
        OrganizationAccess access,
        CancellationToken cancellationToken)
    {
        var member = MemberTargets.Visible(
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

        if (!member.Value.Roles.Contains(command.Role))
        {
            return false;
        }

        var revokingAdmin = command.Role is SchoolRoleNames.SchoolAdmin or SchoolRoleNames.DistrictAdmin;

        return revokingAdmin
            && await context.IsLastEnabledAdminAsync(command.AccountId, command.OrganizationId, cancellationToken)
                ? MemberErrors.LastAdmin
                : true;
    }
}

using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.GrantMemberRole;

internal sealed class GrantMemberRoleHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<GrantMemberRoleHandler> logger) : ICommandHandler<GrantMemberRoleCommand>
{
    public async ValueTask<Result> HandleAsync(GrantMemberRoleCommand command, CancellationToken cancellationToken)
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

        if (!SchoolRoleNames.All.Contains(command.Role) || !SchoolRoleNames.FitsKind(command.Role, access.Value.Kind))
        {
            return Result.Failure(MemberErrors.RoleUnknown);
        }

        var now = timeProvider.GetUtcNow();
        var member = await context.FindMemberAsync(
            command.OrganizationId,
            access.Value.DistrictId,
            command.AccountId,
            now,
            cancellationToken);

        if (CheckGrant(member, command, access.Value) is { } refusal)
        {
            return Result.Failure(refusal);
        }

        if (member!.Roles.Contains(command.Role))
        {
            return Result.Success();
        }

        var granted = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            [],
            command.AccountId,
            lockedAccess => GrantAsync(command, lockedAccess, now));

        if (granted.IsFailure)
        {
            return granted;
        }

        if (granted.Value)
        {
            logger.LogInformation(
                "Account {ActorId} granted {Role} at {OrganizationId} to account {AccountId} in session {SessionId}.",
                command.ActorId,
                command.Role,
                command.OrganizationId,
                command.AccountId,
                command.ActorSessionId);
        }

        return Result.Success();
    }

    private async Task<Result<bool>> GrantAsync(
        GrantMemberRoleCommand command,
        OrganizationAccess access,
        DateTimeOffset now)
    {
        var member = await context.FindMemberAsync(
            command.OrganizationId,
            access.DistrictId,
            command.AccountId,
            now,
            CancellationToken.None);

        if (CheckGrant(member, command, access) is { } refusal)
        {
            return refusal;
        }

        if (member!.Roles.Contains(command.Role))
        {
            return false;
        }

        if (await context.GrantMemberRoleAsync(command.AccountId, command.OrganizationId, command.Role) != 1)
        {
            throw new InvalidOperationException("The role was not granted.");
        }

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.RoleGranted,
            command.AccountId,
            command.Role,
            command.ActorId,
            command.ActorSessionId);

        return true;
    }

    private static Error? CheckGrant(SchoolMember? member, GrantMemberRoleCommand command, OrganizationAccess access)
    {
        var grantingStudent = command.Role == SchoolRoleNames.Student;
        var districtAccount = member is not null && member.OrganizationId != command.OrganizationId;

        return member switch
        {
            null => MemberErrors.NotFound,
            _ when districtAccount && member.Roles.Count == 0 && !access.IsDistrictAdmin => MemberErrors.NotFound,
            _ when grantingStudent && districtAccount => MemberErrors.StudentAtOwnSchoolOnly,
            _ when member.Roles.Any(held => (held == SchoolRoleNames.Student) != grantingStudent) =>
                MemberErrors.StudentRoleExclusive,
            _ => null
        };
    }
}

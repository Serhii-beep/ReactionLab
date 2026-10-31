using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Persistence.AccountSetups;

public static class AccountSetupQueries
{
    public static Task<PendingAccountSetup?> FindPendingAccountSetupAsync(
        this AppDbContext context,
        byte[] tokenHash,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var organizations = context.Set<Organization>();

        return context.Set<AccountSetup>().AsNoTracking()
            .Where(setup => setup.TokenHash == tokenHash && setup.ExpiresAt > now)
            .Join(context.Users, setup => setup.UserId, user => user.Id, (setup, user) => new { setup, user })
            .Join(
                organizations,
                pending => pending.user.OrganizationId,
                organization => (Guid?)organization.Id,
                (pending, organization) => new { pending.setup, pending.user, organization })
            .Where(pending => pending.organization.SuspendedAt == null
                && !organizations.Any(district => district.Id == pending.organization.DistrictId
                    && district.SuspendedAt != null))
            .Select(pending => new PendingAccountSetup(
                pending.user.Id,
                pending.user.DisplayName,
                pending.user.OrganizationUsername!,
                pending.organization.Name,
                pending.organization.SignInCode,
                pending.setup.ExpiresAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static Task<AdminSetupState?> FindAdminSetupStateAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var schoolRoles = context.Set<UserSchoolRole>();
        var districtId = context.Set<Organization>()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => organization.DistrictId);
        var ownAdminRoles = schoolRoles
            .Where(schoolRole => schoolRole.OrganizationId == organizationId
                && (schoolRole.Role == SchoolRoleNames.SchoolAdmin
                    || schoolRole.Role == SchoolRoleNames.DistrictAdmin));
        var administeringRoles = schoolRoles
            .Where(schoolRole => (schoolRole.OrganizationId == organizationId
                    && (schoolRole.Role == SchoolRoleNames.SchoolAdmin
                        || schoolRole.Role == SchoolRoleNames.DistrictAdmin))
                || (schoolRole.Role == SchoolRoleNames.DistrictAdmin
                    && schoolRole.OrganizationId == districtId.FirstOrDefault()));

        return context.Users.AsNoTracking()
            .Where(user => user.Id == accountId
                && user.OrganizationId == organizationId
                && ownAdminRoles.Any(schoolRole => schoolRole.UserId == user.Id))
            .Select(user => new AdminSetupState(
                user.OrganizationUsername!,
                user.DisabledAt != null,
                administeringRoles.Any(schoolRole => context.Users.Any(admin => admin.Id == schoolRole.UserId
                    && admin.PasswordHash != null
                    && admin.DisabledAt == null
                    && admin.LockoutEnd != DateTimeOffset.MaxValue))))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static Task<CompletedAccountSetup?> FindCompletedAccountSetupAsync(
        this AppDbContext context,
        Guid accountId,
        string passwordHash) =>
        context.Users.AsNoTracking()
            .Where(user => user.Id == accountId && user.OrganizationId != null && user.PasswordHash == passwordHash)
            .Select(user => new CompletedAccountSetup(user.Id, user.OrganizationId!.Value))
            .SingleOrDefaultAsync(CancellationToken.None);
}

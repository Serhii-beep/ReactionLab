using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class SchoolAccountQueries
{
    public static Task<User?> FindSchoolAccountAsync(
        this AppDbContext context,
        string signInCode,
        string username,
        CancellationToken cancellationToken)
    {
        var organizations = context.Set<Organization>();

        return context.Users
            .Where(user => user.OrganizationUsername == username && user.DisabledAt == null
                && organizations.Any(organization => organization.Id == user.OrganizationId
                    && organization.SignInCode == signInCode
                    && organization.SuspendedAt == null
                    && !organizations.Any(district => district.Id == organization.DistrictId
                        && district.SuspendedAt != null)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static async Task<SchoolAccountAccess?> FindSchoolAccountAccessAsync(
        this AppDbContext context,
        Guid accountId,
        Guid? preferredOrganizationId,
        CancellationToken cancellationToken)
    {
        var organizations = context.Set<Organization>();
        var unsuspended = organizations.Where(organization => organization.SuspendedAt == null
            && !organizations.Any(district => district.Id == organization.DistrictId && district.SuspendedAt != null));

        var account = await context.Users.AsNoTracking()
            .Where(user => user.Id == accountId && user.OrganizationId != null && user.DisabledAt == null)
            .Select(user => new
            {
                HomeId = user.OrganizationId!.Value,
                HomeIsUnsuspended = unsuspended.Any(organization => organization.Id == user.OrganizationId),
                HeldRoles = context.Set<UserSchoolRole>()
                    .Where(schoolRole => schoolRole.UserId == user.Id
                        && unsuspended.Any(organization => organization.Id == schoolRole.OrganizationId))
                    .OrderBy(schoolRole => schoolRole.OrganizationId)
                    .ThenBy(schoolRole => schoolRole.Role)
                    .Select(schoolRole => new { schoolRole.OrganizationId, schoolRole.Role })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return null;
        }

        var organizationIds = account.HeldRoles.Select(held => held.OrganizationId).Distinct().ToList();
        Guid? activeOrganizationId = preferredOrganizationId is { } preferred && organizationIds.Contains(preferred)
            ? preferred
            : organizationIds.Contains(account.HomeId) ? account.HomeId
            : organizationIds.Count > 0 ? organizationIds[0]
            : account.HomeIsUnsuspended ? account.HomeId
            : null;

        return activeOrganizationId is { } organizationId
            ? new SchoolAccountAccess(
                organizationId,
                [.. account.HeldRoles.Where(held => held.OrganizationId == organizationId).Select(held => held.Role)])
            : null;
    }

    public static async Task<IReadOnlyList<AccountOrganization>> ListAccountOrganizationsAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var organizations = context.Set<Organization>();
        var heldRoles = await context.Set<UserSchoolRole>().AsNoTracking()
            .Where(schoolRole => schoolRole.UserId == accountId)
            .Join(
                organizations,
                schoolRole => schoolRole.OrganizationId,
                organization => organization.Id,
                (schoolRole, organization) => new
                {
                    organization.Id,
                    organization.Kind,
                    organization.Name,
                    IsSuspended = organization.SuspendedAt != null
                        || organizations.Any(district => district.Id == organization.DistrictId
                            && district.SuspendedAt != null),
                    schoolRole.Role
                })
            .OrderBy(held => held.Id)
            .ThenBy(held => held.Role)
            .ToListAsync(cancellationToken);

        return heldRoles
            .GroupBy(held => (held.Id, held.Kind, held.Name, held.IsSuspended))
            .Select(organization => new AccountOrganization(
                organization.Key.Id,
                organization.Key.Kind,
                organization.Key.Name,
                organization.Key.IsSuspended,
                [.. organization.Select(held => held.Role)]))
            .ToList();
    }
}

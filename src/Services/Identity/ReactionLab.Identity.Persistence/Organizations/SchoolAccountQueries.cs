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
            .Where(user => user.OrganizationUsername == username
                && organizations.Any(organization => organization.Id == user.OrganizationId
                    && organization.SignInCode == signInCode
                    && organization.SuspendedAt == null
                    && !organizations.Any(district => district.Id == organization.DistrictId
                        && district.SuspendedAt != null)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public static Task<SchoolAccountAccess?> FindSchoolAccountAccessAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var organizations = context.Set<Organization>();
        var schoolRoles = context.Set<UserSchoolRole>();

        return context.Users.AsNoTracking()
            .Where(user => user.Id == accountId)
            .Join(
                organizations,
                user => user.OrganizationId,
                organization => (Guid?)organization.Id,
                (_, organization) => organization)
            .Select(organization => new SchoolAccountAccess(
                organization.Id,
                organization.SuspendedAt != null
                    || organizations.Any(district => district.Id == organization.DistrictId
                        && district.SuspendedAt != null),
                schoolRoles
                    .Where(schoolRole => schoolRole.UserId == accountId && schoolRole.OrganizationId == organization.Id)
                    .OrderBy(schoolRole => schoolRole.Role)
                    .Select(schoolRole => schoolRole.Role)
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

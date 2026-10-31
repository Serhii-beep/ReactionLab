using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class OrganizationAccessQueries
{
    public static Task<OrganizationAccess?> FindOrganizationAccessAsync(
        this AppDbContext context,
        Guid accountId,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organizations = context.Set<Organization>();
        var heldRoles = context.Set<UserSchoolRole>()
            .Where(schoolRole => schoolRole.UserId == accountId
                && context.Users.Any(user => user.Id == accountId && user.DisabledAt == null));

        return organizations.AsNoTracking()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => new
            {
                organization,
                IsSuspended = organization.SuspendedAt != null
                    || organizations.Any(district => district.Id == organization.DistrictId
                        && district.SuspendedAt != null),
                IsSchoolAdmin = heldRoles.Any(schoolRole => schoolRole.OrganizationId == organization.Id
                    && schoolRole.Role == SchoolRoleNames.SchoolAdmin),
                IsDistrictAdmin = heldRoles.Any(schoolRole => schoolRole.Role == SchoolRoleNames.DistrictAdmin
                    && (schoolRole.OrganizationId == organization.Id
                        || schoolRole.OrganizationId == organization.DistrictId)),
                IsTeacher = heldRoles.Any(schoolRole => schoolRole.OrganizationId == organization.Id
                    && schoolRole.Role == SchoolRoleNames.Teacher)
            })
            .Select(organizationWithRoles => new OrganizationAccess(
                organizationWithRoles.organization.Id,
                organizationWithRoles.organization.Kind,
                organizationWithRoles.organization.DistrictId,
                organizationWithRoles.IsSuspended,
                organizationWithRoles.IsSchoolAdmin || organizationWithRoles.IsDistrictAdmin,
                organizationWithRoles.IsDistrictAdmin,
                organizationWithRoles.IsTeacher,
                organizationWithRoles.organization.TeachersMayResetStudentPasswords,
                organizationWithRoles.organization.Locale))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

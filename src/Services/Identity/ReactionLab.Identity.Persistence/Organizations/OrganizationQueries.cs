using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class OrganizationQueries
{
    public static Task<Organization?> FindOrganizationAsync(
        this AppDbContext context,
        Guid organizationId,
        CancellationToken cancellationToken) =>
        context.Set<Organization>().AsNoTracking()
            .SingleOrDefaultAsync(organization => organization.Id == organizationId, cancellationToken);

    public static Task<List<Organization>> ListOrganizationsAsync(
        this AppDbContext context,
        Guid? districtId,
        Guid after,
        int count,
        CancellationToken cancellationToken)
    {
        var organizations = context.Set<Organization>().AsNoTracking()
            .Where(organization => organization.Id > after);

        if (districtId is { } parentDistrictId)
        {
            organizations = organizations.Where(organization => organization.DistrictId == parentDistrictId);
        }

        return organizations
            .OrderBy(organization => organization.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public static Task<bool> IsDistrictAsync(
        this AppDbContext context,
        Guid organizationId,
        CancellationToken cancellationToken) =>
        context.Set<Organization>().AnyAsync(
            organization => organization.Id == organizationId && organization.Kind == OrganizationKinds.District,
            cancellationToken);

    public static Task<bool> IsSignInCodeTakenAsync(
        this AppDbContext context,
        string signInCode,
        CancellationToken cancellationToken) =>
        context.Set<Organization>().AnyAsync(organization => organization.SignInCode == signInCode, cancellationToken);
}

using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class OrganizationChangeQueries
{
    public static Task<List<OrganizationChange>> ListOrganizationChangesAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid? before,
        int count,
        CancellationToken cancellationToken)
    {
        var changes = context.Set<OrganizationChange>().AsNoTracking()
            .Where(change => change.OrganizationId == organizationId);

        if (before is { } beforeId)
        {
            changes = changes.Where(change => change.Id < beforeId);
        }

        return changes
            .OrderByDescending(change => change.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}

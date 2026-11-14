using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Persistence.Accounts;

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

    public static Task<List<NoticeRecipient>> ListOrganizationAdminRecipientsAsync(
        this AppDbContext context,
        Guid organizationId,
        IReadOnlyCollection<Guid> changedAccountIds,
        CancellationToken cancellationToken)
    {
        var adminIds = AdminIdsOf(context, organizationId);

        return context.Users.AsNoTracking()
            .Where(user => user.DisabledAt == null
                && user.NormalizedEmail != null
                && user.PasswordHash != null
                && (changedAccountIds.Contains(user.Id) || adminIds.Contains(user.Id)))
            .OrderBy(user => user.Id)
            .Select(user => new NoticeRecipient(user.Id, user.Version))
            .ToListAsync(cancellationToken);
    }

    public static async Task<OrganizationChangeDescription?> FindOrganizationChangeAsync(
        this AppDbContext context,
        Guid recipientId,
        Guid changeId,
        CancellationToken cancellationToken)
    {
        var change = await context.Set<OrganizationChange>().AsNoTracking()
            .SingleOrDefaultAsync(recorded => recorded.Id == changeId, cancellationToken);

        var adminIds = AdminIdsOf(context, change?.OrganizationId ?? Guid.Empty);

        if (change is null
            || (change.Change != OrganizationChangeNames.AccountDeleted
                && !SchoolAdminAnnouncements.IsAdminRole(change.Role))
            || (change.AccountId != recipientId
                && !await context.Users.AnyAsync(
                    user => user.Id == recipientId && user.DisabledAt == null && adminIds.Contains(user.Id),
                    cancellationToken)))
        {
            return null;
        }

        var accountCount = change.BatchId is { } batchId
            ? await context.Set<OrganizationChange>()
                .CountAsync(
                    recorded => recorded.BatchId == batchId
                        && recorded.OrganizationId == change.OrganizationId
                        && recorded.Change == change.Change
                        && recorded.Role == change.Role,
                    cancellationToken)
            : 1;

        return await context.Set<Organization>().AsNoTracking()
            .Where(organization => organization.Id == change.OrganizationId)
            .Select(organization => new OrganizationChangeDescription(
                change.ChangedAt,
                organization.Name,
                organization.Kind,
                change.Change,
                change.Role,
                context.Users
                    .Where(user => user.Id == change.AccountId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                context.Users
                    .Where(user => user.Id == change.AccountId)
                    .Select(user => user.OrganizationUsername)
                    .FirstOrDefault(),
                context.Users
                    .Where(user => user.Id == change.ChangedBy)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                context.Users.Any(user => user.Id == change.ChangedBy && user.OrganizationId == null),
                accountCount))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<Guid> AdminIdsOf(AppDbContext context, Guid organizationId)
    {
        var districtId = context.Set<Organization>()
            .Where(organization => organization.Id == organizationId)
            .Select(organization => organization.DistrictId);

        return context.Set<UserSchoolRole>()
            .Where(schoolRole => (schoolRole.OrganizationId == organizationId
                    && (schoolRole.Role == SchoolRoleNames.SchoolAdmin
                        || schoolRole.Role == SchoolRoleNames.DistrictAdmin))
                || (schoolRole.Role == SchoolRoleNames.DistrictAdmin
                    && schoolRole.OrganizationId == districtId.FirstOrDefault()))
            .Select(schoolRole => schoolRole.UserId);
    }
}

using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class MemberQueries
{
    private const string LikeEscape = "\\";

    public static Task<List<SchoolMember>> ListMembersAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid? districtId,
        MemberSearch search,
        Guid after,
        int count,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var schoolRoles = context.Set<UserSchoolRole>();
        var members = context.Users.AsNoTracking()
            .Where(user => user.OrganizationId == organizationId && user.Id > after);

        if (districtId is not null && search.Role != SchoolRoleNames.Student)
        {
            var districtAccountIds = context.Users
                .Where(user => user.OrganizationId == districtId && user.Id > after
                    && schoolRoles.Any(schoolRole => schoolRole.UserId == user.Id
                        && schoolRole.OrganizationId == organizationId))
                .Select(user => user.Id);

            members = members.Concat(context.Users.AsNoTracking().Where(user => districtAccountIds.Contains(user.Id)));
        }

        if (search.Text is { } text)
        {
            var escaped = text.Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
                .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
                .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

            members = members.Where(user => EF.Functions.Like(user.OrganizationUsername!, escaped + "%", LikeEscape)
                || EF.Functions.ILike(user.DisplayName, "%" + escaped + "%", LikeEscape));
        }

        if (search.Role is { } role)
        {
            members = members.Where(user => schoolRoles.Any(schoolRole => schoolRole.UserId == user.Id
                && schoolRole.OrganizationId == organizationId
                && schoolRole.Role == role));
        }

        members = search.Status switch
        {
            MemberStatuses.Disabled => members.Where(user => user.DisabledAt != null),
            MemberStatuses.Pending => members.Where(user => user.DisabledAt == null && user.PasswordHash == null),
            MemberStatuses.Locked => members.Where(user => user.DisabledAt == null && user.PasswordHash != null
                && user.LockoutEnd > now),
            MemberStatuses.Active => members.Where(user => user.DisabledAt == null && user.PasswordHash != null
                && (user.LockoutEnd == null || user.LockoutEnd <= now)),
            _ => members
        };

        var pageAccountIds = members.OrderBy(user => user.Id).Take(count).Select(user => user.Id);
        var accounts = context.Users.AsNoTracking()
            .Where(user => pageAccountIds.Contains(user.Id))
            .OrderBy(user => user.Id);

        return Describe(context, accounts, organizationId, now).ToListAsync(cancellationToken);
    }

    public static Task<SchoolMember?> FindMemberAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid? districtId,
        Guid accountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accounts = context.Users.AsNoTracking()
            .Where(user => user.Id == accountId
                && (user.OrganizationId == organizationId
                    || (districtId != null && user.OrganizationId == districtId)));

        return Describe(context, accounts, organizationId, now).SingleOrDefaultAsync(cancellationToken);
    }

    public static Task<List<SchoolMember>> FindMembersAsync(
        this AppDbContext context,
        Guid organizationId,
        IReadOnlyCollection<Guid> accountIds,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var accounts = context.Users.AsNoTracking()
            .Where(user => accountIds.Contains(user.Id) && user.OrganizationId == organizationId);

        return Describe(context, accounts, organizationId, now).ToListAsync(cancellationToken);
    }

    public static Task<List<string>> ListTakenUsernamesAsync(
        this AppDbContext context,
        Guid organizationId,
        IReadOnlyCollection<string> usernames,
        CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(user => user.OrganizationId == organizationId && usernames.Contains(user.OrganizationUsername!))
            .Select(user => user.OrganizationUsername!)
            .ToListAsync(cancellationToken);

    public static Task<List<Guid>> ListAdminOrganizationIdsAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.Set<UserSchoolRole>().AsNoTracking()
            .Where(schoolRole => schoolRole.UserId == accountId
                && (schoolRole.Role == SchoolRoleNames.SchoolAdmin || schoolRole.Role == SchoolRoleNames.DistrictAdmin))
            .Select(schoolRole => schoolRole.OrganizationId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public static Task<bool> IsLastEnabledAdminAsync(
        this AppDbContext context,
        Guid accountId,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var schoolRoles = context.Set<UserSchoolRole>();

        return schoolRoles.AnyAsync(
            held => held.UserId == accountId
                && (organizationId == null || held.OrganizationId == organizationId)
                && context.Users.Any(admin => admin.Id == accountId && admin.DisabledAt == null)
                && (held.Role == SchoolRoleNames.SchoolAdmin || held.Role == SchoolRoleNames.DistrictAdmin)
                && !schoolRoles.Any(other => other.OrganizationId == held.OrganizationId
                    && other.Role == held.Role
                    && other.UserId != accountId
                    && context.Users.Any(admin => admin.Id == other.UserId && admin.DisabledAt == null)),
            cancellationToken);
    }

    public static Task<bool> AccountExistsAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.Users.AnyAsync(user => user.Id == accountId, cancellationToken);

    private static IQueryable<SchoolMember> Describe(
        AppDbContext context,
        IQueryable<User> accounts,
        Guid organizationId,
        DateTimeOffset now)
    {
        var schoolRoles = context.Set<UserSchoolRole>();
        var setups = context.Set<AccountSetup>();

        return accounts.Select(user => new SchoolMember(
            user.Id,
            user.OrganizationId!.Value,
            user.OrganizationUsername!,
            user.DisplayName,
            user.Email,
            schoolRoles
                .Where(schoolRole => schoolRole.UserId == user.Id && schoolRole.OrganizationId == organizationId)
                .OrderBy(schoolRole => schoolRole.Role)
                .Select(schoolRole => schoolRole.Role)
                .ToList(),
            user.PasswordHash != null,
            setups.Where(setup => setup.UserId == user.Id)
                .Select(setup => (DateTimeOffset?)setup.ExpiresAt)
                .FirstOrDefault() > now,
            user.DisabledAt,
            user.LockoutEnd,
            user.CreatedAt));
    }
}

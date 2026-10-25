using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.PlatformRoles;

public static class PlatformRoleQueries
{
    public static async Task<List<string>> ListPlatformRolesAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var roles = context.Set<IdentityRole<Guid>>();

        var names = await context.Set<IdentityUserRole<Guid>>().AsNoTracking()
            .Where(userRole => userRole.UserId == accountId)
            .Join(roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        return [.. names.Where(PlatformRoleNames.IsKnown)];
    }

    public static Task<bool> HoldsPlatformRoleAsync(
        this AppDbContext context,
        Guid accountId,
        string role,
        CancellationToken cancellationToken)
    {
        var roles = context.Set<IdentityRole<Guid>>();

        return context.Set<IdentityUserRole<Guid>>().AnyAsync(
            userRole => userRole.UserId == accountId
                && roles.Any(platformRole => platformRole.Id == userRole.RoleId && platformRole.Name == role)
                && context.Users.Any(user => user.Id == accountId && user.OrganizationId == null),
            cancellationToken);
    }

    public static Task<int> CountPlatformAdminsWithEstablishedPasswordAsync(
        this AppDbContext context,
        Guid excludedAccountId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var roles = context.Set<IdentityRole<Guid>>();

        return context.Set<IdentityUserRole<Guid>>()
            .Where(userRole => userRole.UserId != excludedAccountId
                && roles.Any(role => role.Id == userRole.RoleId && role.Name == PlatformRoleNames.Admin))
            .Join(context.Users, userRole => userRole.UserId, user => user.Id, (_, user) => user)
            .CountAsync(
                user => user.OrganizationId == null
                    && user.PasswordHash != null
                    && (user.PasswordProbationEndsAt == null || user.PasswordProbationEndsAt <= now),
                cancellationToken);
    }

    public static Task<bool> HasPlatformRoleChangesAsync(
        this AppDbContext context,
        CancellationToken cancellationToken) =>
        context.Set<PlatformRoleChange>().AnyAsync(cancellationToken);

    public static Task<bool> HasBreakGlassGrantAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.Set<PlatformRoleChange>().AnyAsync(
            change => change.AccountId == accountId
                && change.Role == PlatformRoleNames.Admin
                && change.Granted
                && change.Source == PlatformRoleChangeAuthor.BreakGlassSource,
            cancellationToken);

    public static Task<PlatformRoleCandidate?> FindPlatformRoleCandidateAsync(
        this AppDbContext context,
        Guid accountId,
        CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(user => user.Id == accountId && user.OrganizationId == null)
            .Select(user => new PlatformRoleCandidate(user.Id, user.EmailConfirmed))
            .SingleOrDefaultAsync(cancellationToken);

    public static Task<PlatformRoleCandidate?> FindPlatformRoleCandidateByEmailAsync(
        this AppDbContext context,
        string normalizedEmail,
        CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(user => user.NormalizedEmail == normalizedEmail && user.OrganizationId == null)
            .Select(user => new PlatformRoleCandidate(user.Id, user.EmailConfirmed))
            .SingleOrDefaultAsync(cancellationToken);

    public static Task<List<PlatformRoleAccount>> ListPlatformRoleAccountsAsync(
        this AppDbContext context,
        Guid after,
        int count,
        CancellationToken cancellationToken)
    {
        var roles = context.Set<IdentityRole<Guid>>();
        var userRoles = context.Set<IdentityUserRole<Guid>>();

        var pageAccountIds = userRoles
            .Select(userRole => userRole.UserId)
            .Where(accountId => accountId > after)
            .Distinct()
            .OrderBy(accountId => accountId)
            .Take(count);

        return context.Users.AsNoTracking()
            .Where(user => pageAccountIds.Contains(user.Id))
            .OrderBy(user => user.Id)
            .Select(user => new PlatformRoleAccount(
                user.Id,
                user.DisplayName,
                user.Email,
                user.EmailConfirmed,
                userRoles
                    .Where(userRole => userRole.UserId == user.Id)
                    .Join(roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Name!)
                    .OrderBy(name => name)
                    .ToArray()))
            .ToListAsync(cancellationToken);
    }

    public static Task<List<PlatformRoleChangeEntry>> ListPlatformRoleChangesAsync(
        this AppDbContext context,
        Guid? accountId,
        Guid before,
        int count,
        CancellationToken cancellationToken)
    {
        var changes = context.Set<PlatformRoleChange>().AsNoTracking().Where(change => change.Id < before);

        if (accountId is { } changedAccountId)
        {
            changes = changes.Where(change => change.AccountId == changedAccountId);
        }

        return changes
            .OrderByDescending(change => change.Id)
            .Take(count)
            .Select(change => new PlatformRoleChangeEntry(
                change.Id,
                change.AccountId,
                context.Users
                    .Where(user => user.Id == change.AccountId)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                change.Role,
                change.Granted,
                change.Source,
                change.ChangedBy,
                context.Users
                    .Where(user => user.Id == change.ChangedBy)
                    .Select(user => user.DisplayName)
                    .FirstOrDefault(),
                change.ChangedAt))
            .ToListAsync(cancellationToken);
    }
}

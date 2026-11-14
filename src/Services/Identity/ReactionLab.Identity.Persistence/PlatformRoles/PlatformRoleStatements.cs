using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.PlatformRoles;

public static class PlatformRoleStatements
{
    public static Task<int> LockPlatformRoleChangesAsync(this AppDbContext context) =>
        context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM platform_roles WHERE name = {PlatformRoleNames.Admin} FOR UPDATE",
            CancellationToken.None);

    public static async Task<Guid?> GrantPlatformRoleAsync(
        this AppDbContext context,
        Guid accountId,
        string role,
        PlatformRoleChangeAuthor author)
    {
        var changeIds = await context.Database.SqlQuery<Guid>(
            $"""
            WITH granted AS (
                INSERT INTO user_platform_roles (user_id, role_id)
                SELECT users.id, platform_roles.id FROM users CROSS JOIN platform_roles
                WHERE users.id = {accountId} AND users.organization_id IS NULL AND platform_roles.name = {role}
                ON CONFLICT DO NOTHING
                RETURNING user_id)
            INSERT INTO platform_role_changes
                (id, account_id, role, granted, source, changed_by, changed_in_session, changed_at)
            SELECT uuidv7(), user_id, {role}, true, {author.Source}, {author.AccountId}::uuid,
                {author.SessionId}::uuid, clock_timestamp()
            FROM granted
            RETURNING id AS "Value"
            """)
            .ToListAsync(CancellationToken.None);

        return changeIds.Count == 0 ? null : changeIds[0];
    }

    public static async Task<Guid?> RevokePlatformRoleAsync(
        this AppDbContext context,
        Guid accountId,
        string role,
        PlatformRoleChangeAuthor author)
    {
        var changeIds = await context.Database.SqlQuery<Guid>(
            $"""
            WITH revoked AS (
                DELETE FROM user_platform_roles USING platform_roles
                WHERE user_platform_roles.role_id = platform_roles.id AND platform_roles.name = {role}
                    AND user_platform_roles.user_id = {accountId}
                RETURNING user_platform_roles.user_id)
            INSERT INTO platform_role_changes
                (id, account_id, role, granted, source, changed_by, changed_in_session, changed_at)
            SELECT uuidv7(), user_id, {role}, false, {author.Source}, {author.AccountId}::uuid,
                {author.SessionId}::uuid, clock_timestamp()
            FROM revoked
            RETURNING id AS "Value"
            """)
            .ToListAsync(CancellationToken.None);

        return changeIds.Count == 0 ? null : changeIds[0];
    }
}

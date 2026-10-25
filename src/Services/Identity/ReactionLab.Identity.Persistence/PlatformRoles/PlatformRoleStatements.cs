using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.PlatformRoles;

public static class PlatformRoleStatements
{
    public static Task<int> LockPlatformRoleChangesAsync(this AppDbContext context) =>
        context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM platform_roles WHERE name = {PlatformRoleNames.Admin} FOR UPDATE",
            CancellationToken.None);

    public static Task<int> GrantPlatformRoleAsync(
        this AppDbContext context,
        Guid accountId,
        string role,
        PlatformRoleChangeAuthor author) =>
        context.Database.ExecuteSqlAsync(
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
            """,
            CancellationToken.None);

    public static Task<int> RevokePlatformRoleAsync(
        this AppDbContext context,
        Guid accountId,
        string role,
        PlatformRoleChangeAuthor author) =>
        context.Database.ExecuteSqlAsync(
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
            """,
            CancellationToken.None);
}

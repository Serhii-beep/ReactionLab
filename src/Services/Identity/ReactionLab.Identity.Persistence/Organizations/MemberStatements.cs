using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class MemberStatements
{
    public static Task<int> LockOrganizationsAsync(this AppDbContext context, IReadOnlyCollection<Guid> organizationIds)
    {
        var sortedOrganizationIds = organizationIds.Distinct().Order().ToArray();

        return context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM organizations WHERE id = ANY ({sortedOrganizationIds}) ORDER BY id FOR NO KEY UPDATE",
            CancellationToken.None);
    }

    public static Task<int> LockMemberAsync(this AppDbContext context, Guid accountId) =>
        context.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM users WHERE id = {accountId} FOR NO KEY UPDATE",
            CancellationToken.None);

    public static Task<int> GrantMemberRoleAsync(
        this AppDbContext context,
        Guid accountId,
        Guid organizationId,
        string role) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO user_school_roles (user_id, organization_id, role)
            SELECT users.id, organizations.id, {role}
            FROM users, organizations
            WHERE users.id = {accountId} AND organizations.id = {organizationId}
                AND (users.organization_id = organizations.id
                    OR (users.organization_id = organizations.district_id AND {role} <> {SchoolRoleNames.Student}))
                AND ({role} = {SchoolRoleNames.DistrictAdmin}) = (organizations.kind = {OrganizationKinds.District})
                AND NOT EXISTS (
                    SELECT 1 FROM user_school_roles AS held
                    WHERE held.user_id = users.id AND held.organization_id = organizations.id
                        AND (held.role = {SchoolRoleNames.Student}) <> ({role} = {SchoolRoleNames.Student}))
            ON CONFLICT DO NOTHING
            """,
            CancellationToken.None);

    public static Task<int> RevokeMemberRoleAsync(
        this AppDbContext context,
        Guid accountId,
        Guid organizationId,
        string role) =>
        context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM user_school_roles
            WHERE user_id = {accountId} AND organization_id = {organizationId} AND role = {role}
                AND (role NOT IN ({SchoolRoleNames.SchoolAdmin}, {SchoolRoleNames.DistrictAdmin}) OR EXISTS (
                    SELECT 1 FROM user_school_roles AS other JOIN users ON users.id = other.user_id
                    WHERE other.organization_id = {organizationId} AND other.role = {role}
                        AND other.user_id <> {accountId} AND users.disabled_at IS NULL))
            """,
            CancellationToken.None);

    public static async Task<RenamedMember?> RenameMemberAsync(
        this AppDbContext context,
        Guid accountId,
        Guid organizationId,
        string? displayName,
        string? username) =>
        (await context.Database.SqlQuery<RenamedMember>(
            $"""
            UPDATE users
            SET display_name = coalesce({displayName}, display_name),
                organization_username = coalesce({username}, organization_username),
                version = version + CASE
                    WHEN coalesce({displayName}, display_name) IS DISTINCT FROM display_name THEN 1 ELSE 0 END,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {accountId} AND organization_id = {organizationId}
            RETURNING display_name, preferred_locale, version, version <> old.version AS display_name_changed
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();

    public static Task<int> DisableMemberAsync(this AppDbContext context, Guid accountId, Guid organizationId) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH cleared AS (
                DELETE FROM account_setups
                WHERE user_id = {accountId}
                    AND EXISTS (SELECT 1 FROM users WHERE id = {accountId} AND organization_id = {organizationId}))
            UPDATE users
            SET disabled_at = clock_timestamp(),
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {accountId} AND organization_id = {organizationId} AND disabled_at IS NULL
            """,
            CancellationToken.None);

    public static Task<int> EnableMemberAsync(this AppDbContext context, Guid accountId, Guid organizationId) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET disabled_at = NULL,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {accountId} AND organization_id = {organizationId} AND disabled_at IS NOT NULL
            """,
            CancellationToken.None);

    public static Task<int> SetMemberPasswordAsync(
        this AppDbContext context,
        Guid accountId,
        Guid organizationId,
        string passwordHash) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH cleared AS (
                DELETE FROM account_setups
                WHERE user_id = {accountId}
                    AND EXISTS (SELECT 1 FROM users WHERE id = {accountId} AND organization_id = {organizationId}))
            UPDATE users
            SET password_hash = {passwordHash},
                password_probation_ends_at = NULL,
                access_failed_count = 0,
                lockout_end = NULL,
                current_password_failures = 0,
                current_password_lockout_end = NULL,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {accountId} AND organization_id = {organizationId}
            """,
            CancellationToken.None);

    public static Task<int> ClearMemberPasswordAsync(this AppDbContext context, Guid accountId, Guid organizationId) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET password_hash = NULL,
                password_probation_ends_at = NULL,
                access_failed_count = 0,
                lockout_end = NULL,
                current_password_failures = 0,
                current_password_lockout_end = NULL,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {accountId} AND organization_id = {organizationId}
            """,
            CancellationToken.None);

    public static async Task<int> EndAccountSessionsAsync(
        this AppDbContext context,
        Guid accountId,
        string securityStamp) =>
        (await context.Database.SqlQuery<int>(
            $"""
            WITH stamped AS (
                UPDATE users
                SET security_stamp = {securityStamp},
                    concurrency_stamp = gen_random_uuid()::text
                WHERE id = {accountId}
                RETURNING id),
            locked AS MATERIALIZED (
                SELECT id FROM openiddict_authorizations
                WHERE subject = {accountId.ToString()} AND status = 'valid'
                ORDER BY id
                FOR NO KEY UPDATE),
            ended AS (
                UPDATE openiddict_authorizations SET status = 'revoked'
                WHERE id IN (SELECT id FROM locked) AND EXISTS (SELECT 1 FROM stamped)
                RETURNING id),
            ended_tokens AS (
                UPDATE openiddict_tokens SET status = 'revoked'
                WHERE authorization_id = ANY (ARRAY(SELECT id FROM ended)) AND status = 'valid')
            SELECT count(*)::int AS "Value" FROM ended
            """)
            .ToListAsync(CancellationToken.None))
            .Single();

    public static Task<int> RecordAccountChangeAsync(
        this AppDbContext context,
        Guid accountId,
        string change,
        Guid changedBy,
        Guid changedInSession) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO organization_changes
                (id, organization_id, change, account_id, role, changed_by, changed_in_session, changed_at)
            SELECT uuidv7(), organization_id, {change}, {accountId}, NULL, {changedBy}, {changedInSession},
                clock_timestamp()
            FROM (
                SELECT organization_id FROM users WHERE id = {accountId}
                UNION
                SELECT organization_id FROM user_school_roles WHERE user_id = {accountId}) AS affected
            """,
            CancellationToken.None);

    public static async Task<long?> DeleteMemberAsync(this AppDbContext context, Guid accountId, Guid organizationId)
    {
        await context.Database.ExecuteSqlAsync(
            $"""
            DELETE FROM user_school_roles
            WHERE user_id = {accountId}
                AND EXISTS (SELECT 1 FROM users WHERE id = {accountId} AND organization_id = {organizationId})
            """,
            CancellationToken.None);

        var deletedVersions = await context.Database
            .SqlQuery<long>(
                $"""
                DELETE FROM users WHERE id = {accountId} AND organization_id = {organizationId}
                RETURNING version AS "Value"
                """)
            .ToListAsync(CancellationToken.None);

        return deletedVersions.Count == 1 ? deletedVersions[0] : null;
    }

    public static async Task<int> TransferMemberAsync(
        this AppDbContext context,
        Guid accountId,
        Guid fromSchoolId,
        Guid toSchoolId)
    {
        var moved = await context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET organization_id = {toSchoolId},
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {accountId} AND organization_id = {fromSchoolId}
                AND EXISTS (
                    SELECT 1 FROM organizations AS origin JOIN organizations AS destination
                        ON destination.district_id = origin.district_id
                    WHERE origin.id = {fromSchoolId} AND destination.id = {toSchoolId}
                        AND destination.kind = {OrganizationKinds.School})
            """,
            CancellationToken.None);

        if (moved == 1)
        {
            await context.Database.ExecuteSqlAsync(
                $"""
                UPDATE user_school_roles SET organization_id = {toSchoolId}
                WHERE user_id = {accountId} AND organization_id = {fromSchoolId}
                """,
                CancellationToken.None);
        }

        return moved;
    }
}

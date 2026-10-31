using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Organizations;

public static class OrganizationStatements
{
    public static Task<int> GrantSchoolRoleAsync(
        this AppDbContext context,
        Guid accountId,
        Guid organizationId,
        string role) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO user_school_roles (user_id, organization_id, role)
            SELECT users.id, organizations.id, {role}
            FROM users JOIN organizations ON organizations.id = users.organization_id
            WHERE users.id = {accountId} AND organizations.id = {organizationId}
                AND ({role} = {SchoolRoleNames.DistrictAdmin}) = (organizations.kind = {OrganizationKinds.District})
            ON CONFLICT DO NOTHING
            """,
            CancellationToken.None);

    public static Task<int> RecordOrganizationChangeAsync(
        this AppDbContext context,
        Guid organizationId,
        string change,
        Guid? accountId,
        string? role,
        Guid? changedBy,
        Guid? changedInSession) =>
        context.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO organization_changes
                (id, organization_id, change, account_id, role, changed_by, changed_in_session, changed_at)
            VALUES (uuidv7(), {organizationId}, {change}, {accountId}, {role}, {changedBy}, {changedInSession},
                clock_timestamp())
            """,
            CancellationToken.None);

    public static Task<int> SuspendOrganizationAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid changedBy,
        Guid changedInSession) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH suspended AS (
                UPDATE organizations SET suspended_at = clock_timestamp()
                WHERE id = {organizationId} AND suspended_at IS NULL
                RETURNING id, suspended_at)
            INSERT INTO organization_changes
                (id, organization_id, change, account_id, role, changed_by, changed_in_session, changed_at)
            SELECT uuidv7(), id, {OrganizationChangeNames.Suspended}, NULL, NULL, {changedBy}, {changedInSession},
                suspended_at
            FROM suspended
            """,
            CancellationToken.None);

    public static Task<int> ReinstateOrganizationAsync(
        this AppDbContext context,
        Guid organizationId,
        Guid changedBy,
        Guid changedInSession) =>
        context.Database.ExecuteSqlAsync(
            $"""
            WITH reinstated AS (
                UPDATE organizations SET suspended_at = NULL
                WHERE id = {organizationId} AND suspended_at IS NOT NULL
                RETURNING id)
            INSERT INTO organization_changes
                (id, organization_id, change, account_id, role, changed_by, changed_in_session, changed_at)
            SELECT uuidv7(), id, {OrganizationChangeNames.Reinstated}, NULL, NULL, {changedBy}, {changedInSession},
                clock_timestamp()
            FROM reinstated
            """,
            CancellationToken.None);
}

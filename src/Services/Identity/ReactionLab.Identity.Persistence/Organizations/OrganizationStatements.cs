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
}

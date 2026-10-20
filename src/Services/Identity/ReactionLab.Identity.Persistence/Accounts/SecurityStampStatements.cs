using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class SecurityStampStatements
{
    public static async Task<string?> ReplaceSecurityStampAsync(
        this AppDbContext context,
        Guid userId,
        string securityStamp) =>
        (await context.Database.SqlQuery<string>(
            $"""
            WITH previous AS MATERIALIZED (
                SELECT id, security_stamp FROM users WHERE id = {userId} FOR NO KEY UPDATE)
            UPDATE users
            SET security_stamp = {securityStamp},
                concurrency_stamp = gen_random_uuid()::text
            FROM previous
            WHERE users.id = previous.id
            RETURNING previous.security_stamp AS "Value"
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();

    public static async Task<string?> ReplacePasswordAndSecurityStampAsync(
        this AppDbContext context,
        Guid userId,
        string previousHash,
        string replacementHash,
        string securityStamp) =>
        (await context.Database.SqlQuery<string>(
            $"""
            WITH previous AS MATERIALIZED (
                SELECT id, security_stamp FROM users WHERE id = {userId} FOR NO KEY UPDATE)
            UPDATE users
            SET password_hash = {replacementHash},
                security_stamp = {securityStamp},
                concurrency_stamp = gen_random_uuid()::text
            FROM previous
            WHERE users.id = previous.id AND users.password_hash IN ({previousHash}, {replacementHash})
            RETURNING previous.security_stamp AS "Value"
            """)
            .ToListAsync(CancellationToken.None))
            .SingleOrDefault();
}

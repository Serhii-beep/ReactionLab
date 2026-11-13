using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class EmailAddressStatements
{
    public static Task<int> RewriteEmailAsync(
        this AppDbContext context,
        StoredEmail stored,
        string canonicalEmail,
        string normalizedEmail) =>
        context.Database.ExecuteSqlAsync(
            $"""
            UPDATE users
            SET email = {canonicalEmail},
                normalized_email = {normalizedEmail},
                user_name = CASE WHEN user_name = email THEN {canonicalEmail} ELSE user_name END,
                normalized_user_name = CASE
                    WHEN user_name = email THEN {normalizedEmail}
                    ELSE normalized_user_name
                END,
                concurrency_stamp = gen_random_uuid()::text
            WHERE id = {stored.AccountId} AND email = {stored.Email}
            """,
            CancellationToken.None);
}

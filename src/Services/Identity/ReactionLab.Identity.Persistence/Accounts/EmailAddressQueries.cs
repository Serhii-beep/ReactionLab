using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class EmailAddressQueries
{
    public static Task<List<StoredEmail>> ListEmailsIdnaCouldRewriteAsync(
        this AppDbContext context,
        CancellationToken cancellationToken) =>
        context.Database.SqlQuery<StoredEmail>(
            $"""
            SELECT id AS account_id, organization_id, email
            FROM users
            WHERE email ~ '[^[:ascii:]]' OR email ~* '(^|[@.])xn--'
            """)
            .ToListAsync(cancellationToken);
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Migrator;

internal sealed class EmailCanonicalization(AppDbContext context, ILogger<EmailCanonicalization> logger)
{
    private static readonly UpperInvariantLookupNormalizer Normalizer = new();

    public async Task CanonicalizeAsync(CancellationToken cancellationToken)
    {
        var candidates = await context.ListEmailsIdnaCouldRewriteAsync(cancellationToken);
        var canonicalized = new List<(StoredEmail Stored, string Canonical)>();

        foreach (var stored in candidates)
        {
            if (User.CanonicalizeEmail(stored.Email) is { IsSuccess: true } canonical)
            {
                canonicalized.Add((stored, canonical.Value));
            }
        }

        var rewritten = 0;

        foreach (var claimants in canonicalized.GroupBy(
            entry => (entry.Stored.OrganizationId, Key: Normalizer.NormalizeEmail(entry.Canonical))))
        {
            if (claimants.Count() > 1)
            {
                var (organizationId, key) = claimants.Key;
                var holderId = await context.Users
                    .Where(user => user.OrganizationId == organizationId && user.NormalizedEmail == key)
                    .Select(user => (Guid?)user.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                logger.LogWarning(
                    "Accounts {AccountIds} share one address once canonicalized, so each is kept as stored; "
                    + "{AddressHolder} holds it.",
                    string.Join(", ", claimants.Select(entry => entry.Stored.AccountId)),
                    holderId?.ToString() ?? "no account");
                continue;
            }

            var (stored, canonical) = claimants.Single();

            if (canonical != stored.Email)
            {
                rewritten += await RewriteAsync(stored, canonical, claimants.Key.Key);
            }
        }

        logger.LogInformation(
            "Canonicalized {RewrittenCount} of {CandidateCount} stored non-ASCII or punycode address(es); "
            + "{RefusedCount} of them the rule refuses, kept as typed.",
            rewritten,
            candidates.Count,
            candidates.Count - canonicalized.Count);
    }

    private async Task<int> RewriteAsync(StoredEmail stored, string canonical, string normalizedEmail)
    {
        try
        {
            return await context.RewriteEmailAsync(stored, canonical, normalizedEmail);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            logger.LogWarning(
                "Account {AccountId}'s address is another account's once canonicalized, so it is kept as stored.",
                stored.AccountId);

            return 0;
        }
    }
}

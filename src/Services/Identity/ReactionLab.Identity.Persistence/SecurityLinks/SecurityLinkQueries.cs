using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.SecurityLinks;

public static class SecurityLinkQueries
{
    public static Task<bool> HoldsEmailConfirmationLinkAsync(
        this AppDbContext context,
        Guid userId,
        byte[] tokenHash,
        CancellationToken cancellationToken) =>
        context.Set<SecurityLink>().AnyAsync(
            link => link.UserId == userId
                && link.Purpose == SecurityLinkPurposes.EmailConfirmation
                && link.TokenHash == tokenHash
                && link.ExpiresAt > DateTimeOffset.UtcNow
                && context.Users.Any(user => user.Id == link.UserId && user.Email == link.Email),
            cancellationToken);
}

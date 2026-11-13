using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Persistence.SecurityLinks;

public sealed class SecurityLink
{
    public const int MaximumPurposeLength = 32;
    public const int MaximumRequestsPerWindow = 5;

    public static readonly TimeSpan EmailConfirmationLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan EmailConfirmationPeriod = TimeSpan.FromDays(7);
    public static readonly TimeSpan RequestLifetime = TimeSpan.FromDays(1);
    public static readonly TimeSpan RequestWindow = TimeSpan.FromDays(1);
    public static readonly TimeSpan MinimumIntervalBetweenRequests = TimeSpan.FromMinutes(5);

    private SecurityLink()
    {

    }

    public Guid UserId { get; private set; }

    public string Purpose { get; private set; } = string.Empty;

    public Guid RequestId { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset FirstRequestedAt { get; private set; }

    public int RequestCount { get; private set; }

    public byte[]? TokenHash { get; private set; }

    public DateTimeOffset? IssuedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public static SecurityLink RequestEmailConfirmation(User account, DateTimeOffset now) =>
        new()
        {
            UserId = account.Id,
            Purpose = SecurityLinkPurposes.EmailConfirmation,
            RequestId = Guid.CreateVersion7(now),
            Email = account.Email ?? throw new InvalidOperationException("The account has no address to confirm."),
            RequestCount = 1
        };
}

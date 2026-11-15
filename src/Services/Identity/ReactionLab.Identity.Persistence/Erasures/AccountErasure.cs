using ReactionLab.Identity.Contracts;

namespace ReactionLab.Identity.Persistence.Erasures;

public sealed class AccountErasure
{
    public const int MaximumReasonLength = 32;
    public const int RetentionYears = 3;

    public static readonly TimeSpan ConfirmationPeriod = TimeSpan.FromDays(7);
    public static readonly IReadOnlyList<string> ConfirmingServices = [ErasureServices.Notifications];

    private AccountErasure()
    {

    }

    public Guid UserId { get; private set; }

    public long Version { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? EscalatedAt { get; private set; }
}

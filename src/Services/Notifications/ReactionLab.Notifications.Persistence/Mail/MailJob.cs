namespace ReactionLab.Notifications.Persistence.Mail;

public sealed class MailJob
{
    public const int MaximumMailTypeLength = 64;

    private MailJob()
    {

    }

    public Guid RecipientId { get; private init; }

    public Guid Id { get; private init; }

    public string MailType { get; private init; } = string.Empty;

    public string? TraceParent { get; private init; }

    public DateTimeOffset AvailableAt { get; private init; }

    public DateTimeOffset CreatedAt { get; private init; }

    public static MailJob Queue(
        Guid recipientId,
        Guid id,
        string mailType,
        string? traceParent,
        DateTimeOffset now) =>
        new()
        {
            RecipientId = recipientId,
            Id = id,
            MailType = mailType,
            TraceParent = traceParent,
            AvailableAt = now,
            CreatedAt = now
        };
}

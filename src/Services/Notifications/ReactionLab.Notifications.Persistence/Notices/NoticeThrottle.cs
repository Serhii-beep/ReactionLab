namespace ReactionLab.Notifications.Persistence.Notices;

public sealed class NoticeThrottle
{
    private NoticeThrottle()
    {

    }

    public Guid RecipientId { get; private init; }

    public string MailType { get; private init; } = string.Empty;

    public DateTimeOffset LastQueuedAt { get; private init; }
}

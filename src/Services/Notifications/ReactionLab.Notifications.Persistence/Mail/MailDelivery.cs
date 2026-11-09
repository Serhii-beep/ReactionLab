namespace ReactionLab.Notifications.Persistence.Mail;

public sealed class MailDelivery
{
    private MailDelivery()
    {

    }

    public Guid RecipientId { get; private init; }

    public Guid Id { get; private init; }

    public string MailType { get; private init; } = string.Empty;

    public DateTimeOffset SentAt { get; private init; }
}

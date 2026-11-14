namespace ReactionLab.Notifications.Mail;

public sealed record MailFacts(IReadOnlyList<MailFact> Facts) : MailBlock;

namespace ReactionLab.Notifications.Mail;

public sealed record MailParagraph(MailText Text, bool IsAside = false) : MailBlock;

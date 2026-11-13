namespace ReactionLab.Notifications.Mail;

public sealed record MailLinkCopy(string Label, Uri Target) : MailBlock;

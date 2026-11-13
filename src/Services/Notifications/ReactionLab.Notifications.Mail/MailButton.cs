namespace ReactionLab.Notifications.Mail;

public sealed record MailButton(string Label, Uri Target, bool IsPrimary = false) : MailBlock;

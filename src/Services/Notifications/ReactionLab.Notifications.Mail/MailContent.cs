namespace ReactionLab.Notifications.Mail;

public sealed record MailContent(
    string Locale,
    string Subject,
    string Heading,
    IReadOnlyList<MailBlock> Blocks,
    IReadOnlyList<string> FooterLines);

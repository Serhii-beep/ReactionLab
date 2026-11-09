namespace ReactionLab.Notifications.Mail;

public sealed record MailArgument(string Name, string Value, bool IsEmphasized = false);

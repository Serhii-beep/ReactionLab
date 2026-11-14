namespace ReactionLab.Notifications.Templates;

internal sealed record CurrentLockout(DateTimeOffset? PausedUntil, bool Disabled, int Failures) : MailDetail;

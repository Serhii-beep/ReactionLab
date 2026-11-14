namespace ReactionLab.Notifications.Templates;

internal sealed record RecordedPasswordChange(DateTimeOffset ChangedAt, string Browser, string OperatingSystem);

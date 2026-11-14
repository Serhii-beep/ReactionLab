namespace ReactionLab.Notifications.Templates;

internal sealed record MailRecipient(
    string? DisplayName,
    string Email,
    string Locale,
    string? TimeZone,
    SecurityLinkToken? Link,
    RecordedPasswordChange? PasswordChange);

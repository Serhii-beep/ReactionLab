using ReactionLab.Identity.Contracts.Contacts.V1;

namespace ReactionLab.Notifications.Templates;

internal sealed record RecordedPlatformRoleChange(
    DateTimeOffset ChangedAt,
    bool Granted,
    PlatformRoleChangeSource Source,
    string AccountDisplayName,
    string AccountEmail,
    string ChangedByDisplayName,
    string ChangedByEmail) : MailDetail;

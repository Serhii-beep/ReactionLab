using ReactionLab.Identity.Contracts.Contacts.V1;

namespace ReactionLab.Notifications.Templates;

internal sealed record RecordedSignInMethodChange(
    DateTimeOffset ChangedAt,
    SignInMethodChangeKind Change,
    string ProviderDisplayName,
    string Browser,
    string OperatingSystem,
    bool HasPassword,
    IReadOnlyList<string> ProviderDisplayNames) : MailDetail;

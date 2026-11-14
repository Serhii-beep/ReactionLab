using ReactionLab.Identity.Contracts.Contacts.V1;

namespace ReactionLab.Notifications.Templates;

internal sealed record RecordedSchoolAdminChange(
    DateTimeOffset ChangedAt,
    string OrganizationName,
    AdminRoleChange Change,
    string Role,
    string AccountDisplayName,
    string AccountUsername,
    string ChangedByDisplayName,
    bool ChangedByPlatform,
    int AccountCount) : MailDetail;

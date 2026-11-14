using ReactionLab.Identity.Contracts.Contacts.V1;

namespace ReactionLab.Notifications.Templates;

internal sealed record MailedAccountSetup(
    AccountSetupPurpose Purpose,
    string OrganizationName,
    string SignInCode,
    string Username,
    IReadOnlyList<string> Roles,
    string RequestedByDisplayName,
    bool RequestedByPlatformAdmin,
    IReadOnlyList<string> RequestedByRoles) : MailDetail;

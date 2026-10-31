using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations;

internal static class OrganizationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Organization.NotFound",
        "No organization has this id.");

    public static readonly Error DistrictNotFound = Error.NotFound(
        "Organization.DistrictNotFound",
        "No district has this id.",
        "DistrictId");

    public static readonly Error AdminNotFound = Error.NotFound(
        "Organization.AdminNotFound",
        "The organization has no admin with this id.");

    public static readonly Error AlreadySetUp = Error.Conflict(
        "Organization.AlreadySetUp",
        "An admin of this organization or its district can sign in, so they issue its setup links.");

    public static readonly Error AdminDisabled = Error.Conflict(
        "Organization.AdminDisabled",
        "This admin's account is disabled, so a setup link wouldn't let it sign in.");

    public static readonly Error AdminRequired = Error.Forbidden(
        "Organization.AdminRequired",
        "Only the organization's admins can do this.");

    public static readonly Error DistrictAdminRequired = Error.Forbidden(
        "Organization.DistrictAdminRequired",
        "Only the district's admins can do this.");

    public static readonly Error Suspended = Error.Forbidden(
        "Organization.Suspended",
        "The organization is suspended.");

    public static readonly Error SignInCodeInUse = Error.Conflict(
        "Organization.SignInCodeInUse",
        "Another organization has this sign-in code.",
        "SignInCode");

    public static readonly Error SettingsRequired = Error.Validation(
        "Organization.SettingsRequired",
        "Give at least one setting to change.");

    public static readonly Error NotSuspended = Error.NotFound(
        "Organization.NotSuspended",
        "The organization isn't suspended.");

    public static readonly Error SignInCodeUnavailable = Error.Unexpected(
        "Organization.SignInCodeUnavailable",
        "No free sign-in code was found.");
}

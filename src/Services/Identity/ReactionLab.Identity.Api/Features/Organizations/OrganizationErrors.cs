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

    public static readonly Error AddressInUse = Error.Conflict(
        "Organization.AddressInUse",
        "Another account uses this email address.",
        "firstAdmin.email");

    public static readonly Error AdminNotFound = Error.NotFound(
        "Organization.AdminNotFound",
        "The organization has no admin with this id.");

    public static readonly Error AlreadySetUp = Error.Conflict(
        "Organization.AlreadySetUp",
        "An admin of this organization has set a password, so its school issues setup links now.");

    public static readonly Error SignInCodeUnavailable = Error.Unexpected(
        "Organization.SignInCodeUnavailable",
        "No free sign-in code was found.");
}

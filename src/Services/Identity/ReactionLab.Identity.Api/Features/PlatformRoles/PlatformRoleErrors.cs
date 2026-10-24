using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.PlatformRoles;

internal static class PlatformRoleErrors
{
    public static readonly Error Unknown = Error.NotFound(
        "PlatformRole.Unknown",
        "No platform role has this name.");

    public static readonly Error AccountNotFound = Error.NotFound(
        "PlatformRole.AccountNotFound",
        "No account matches.");

    public static readonly Error NotGranted = Error.NotFound(
        "PlatformRole.NotGranted",
        "The account doesn't hold this platform role.");

    public static readonly Error AddressUnconfirmed = Error.Conflict(
        "PlatformRole.AddressUnconfirmed",
        "A platform role goes only to an account whose email address is confirmed.");

    public static readonly Error LastAdmin = Error.Conflict(
        "PlatformRole.LastAdmin",
        "Revoking this would leave no admin with an established password, who alone can change platform roles.");

    public static readonly Error AdminRequired = Error.Forbidden(
        "PlatformRole.AdminRequired",
        "Only a platform admin changes platform roles.");
}

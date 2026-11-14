using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.AccountSetups;

internal static class AccountSetupErrors
{
    public static readonly Error LinkInvalid = Error.NotFound(
        "AccountSetup.LinkInvalid",
        "This setup link is not valid: it was used, replaced or has expired.");

    public static readonly Error UsernameRequired = Error.Validation(
        "AccountSetup.UsernameRequired",
        "This account has no username yet, so the setup chooses one.",
        "Username");

    public static readonly Error UsernameInUse = Error.Conflict(
        "AccountSetup.UsernameInUse",
        "Another account at this school has that username.");
}

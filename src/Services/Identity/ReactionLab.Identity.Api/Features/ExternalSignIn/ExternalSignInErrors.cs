using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn;

internal static class ExternalSignInErrors
{
    public const string FailedCode = "ExternalSignIn.Failed";
    public const string CanceledCode = "ExternalSignIn.Canceled";
    public const string UnavailableCode = "ExternalSignIn.Unavailable";

    public static readonly Error UnknownProvider = Error.NotFound(
        "ExternalSignIn.UnknownProvider",
        "No sign-in provider has this name.");

    public static readonly Error ReturnUrlInvalid = Error.Validation(
        "ExternalSignIn.ReturnUrlInvalid",
        "The return address must be a path on this site.",
        "ReturnUrl");

    public static readonly Error NotPending = Error.NotFound(
        "ExternalSignIn.NotPending",
        "No sign-in through a provider is waiting to be finished.");

    public static readonly Error AccountExists = Error.Conflict(
        "ExternalSignIn.AccountExists",
        "An account already uses this email address. Enter its password to connect it.");

    public static readonly Error AlreadyLinked = Error.Conflict(
        "ExternalSignIn.AlreadyLinked",
        "This provider account is already connected to an account. Sign in with it instead.");
}

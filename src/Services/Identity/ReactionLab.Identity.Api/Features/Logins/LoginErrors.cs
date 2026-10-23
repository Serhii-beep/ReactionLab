using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Logins;

internal static class LoginErrors
{
    public static readonly Error NotConnected = Error.NotFound(
        "Login.NotConnected",
        "The account doesn't sign in with this provider.");

    public static readonly Error AlreadyConnected = Error.Conflict(
        "Login.AlreadyConnected",
        "The account already signs in with this provider.");

    public static readonly Error ConnectedElsewhere = Error.Conflict(
        "Login.ConnectedElsewhere",
        "This provider account signs in to another account.");
}

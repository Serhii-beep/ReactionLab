using ReactionLab.Identity.Api.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Sessions;

internal static class SessionErrors
{
    public static readonly Error Ended = Error.Unauthorized(
        "Session.Ended",
        "This session has ended.");

    public static readonly Error NotFound = Error.NotFound(
        "Session.NotFound",
        "The account has no such session.");

    public static readonly Error RecentSignInRequired = Error.Forbidden(
        "Session.RecentSignInRequired",
        "Ending another session needs a sign-in from the last 10 minutes.")
        .WithArgs(("maxAge", (int)SessionPolicy.RecentSignInWindow.TotalSeconds));
}

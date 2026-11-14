using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal static class UserAgents
{
    public static string? Bounded(string? userAgent) =>
        string.IsNullOrEmpty(userAgent)
            ? null
            : userAgent.Length > PasswordChange.MaximumUserAgentLength
                ? userAgent[..PasswordChange.MaximumUserAgentLength]
                : userAgent;
}

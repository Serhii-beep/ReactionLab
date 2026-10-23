using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;

namespace ReactionLab.Identity.Api.Features.Logins;

internal static class LoginConnections
{
    private const string AccountIdProperty = "connecting_account_id";
    private const string SessionIdProperty = "connecting_session_id";
    private const string ErrorParameter = "error";

    public static void Attach(AuthenticationProperties properties, LoginConnection connection)
    {
        properties.Items[AccountIdProperty] = connection.AccountId.ToString();
        properties.Items[SessionIdProperty] = connection.SessionId.ToString();
    }

    public static LoginConnection? Find(AuthenticationProperties? properties) =>
        properties is not null
        && Guid.TryParse(properties.GetString(AccountIdProperty), out var accountId)
        && Guid.TryParse(properties.GetString(SessionIdProperty), out var sessionId)
            ? new LoginConnection(accountId, sessionId)
            : null;

    public static string WithError(string errorCode, string returnUrl) =>
        QueryHelpers.AddQueryString(returnUrl, ErrorParameter, errorCode);
}

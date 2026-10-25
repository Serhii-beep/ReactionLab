using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.AccountSetups;

internal static class AccountSetupErrors
{
    public static readonly Error LinkInvalid = Error.NotFound(
        "AccountSetup.LinkInvalid",
        "This setup link is not valid: it was used, replaced or has expired.");
}

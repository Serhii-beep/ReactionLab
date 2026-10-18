using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal static class AccountErrors
{
    public static readonly Error SignInFailed = Error.Validation(
        "Account.SignInFailed",
        "The email address or the password is incorrect, or there were too many failed attempts.");

    public static Error Rejected(string description) =>
        Error.Validation("Account.Rejected", description);
}

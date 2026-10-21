using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal static class AccountErrors
{
    public static readonly Error SignInFailed = Error.Validation(
        "Account.SignInFailed",
        "The email address or the password is incorrect, or there were too many failed attempts.");

    public static readonly Error NotFound = Error.NotFound(
        "Account.NotFound",
        "The account no longer exists.");

    public static readonly Error CurrentPasswordIncorrect = Error.Validation(
        "Account.CurrentPasswordIncorrect",
        "The current password is incorrect.",
        "CurrentPassword");

    public static readonly Error PasswordNotSet = Error.Conflict(
        "Account.PasswordNotSet",
        "This account has no password.");

    public static readonly Error PasswordCheckLocked = Error.Forbidden(
        "Account.PasswordCheckLocked",
        "Password checks are locked after too many wrong passwords.");

    public static Error Rejected(string description) =>
        Error.Validation("Account.Rejected", description);
}

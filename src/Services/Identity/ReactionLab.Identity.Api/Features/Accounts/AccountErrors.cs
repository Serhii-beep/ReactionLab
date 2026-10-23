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

    public static readonly Error PasswordAlreadySet = Error.Conflict(
        "Account.PasswordAlreadySet",
        "This account already has a password.");

    public static readonly Error LastSignInMethod = Error.Conflict(
        "Account.LastSignInMethod",
        "An account keeps at least one way to sign in that works.");

    public static readonly Error PasswordNeedsEmail = Error.Conflict(
        "Account.PasswordNeedsEmail",
        "A password signs in with an email address, and this account has none.");

    public static readonly Error PasswordSignInDisabled = Error.Conflict(
        "Account.PasswordSignInDisabled",
        "Password sign-in is disabled on this account until a reset.");

    public static Error Rejected(string description) =>
        Error.Validation("Account.Rejected", description);

    public static Error PasswordTooNew(DateTimeOffset probationEndsAt) =>
        Error.Conflict(
            "Account.PasswordTooNew",
            "A password set in the last 7 days can't yet add or replace a way to sign in.")
        .WithArgs(("probationEndsAt", probationEndsAt));
}

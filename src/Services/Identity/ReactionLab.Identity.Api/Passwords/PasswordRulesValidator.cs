using Microsoft.AspNetCore.Identity;
using Polly;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Passwords;

internal sealed class PasswordRulesValidator(
    PwnedPasswordsClient pwnedPasswords,
    ILogger<PasswordRulesValidator> logger) : IPasswordValidator<User>
{
    public static readonly Error InvalidCharacters = Error.Validation(
        "Password.InvalidCharacters",
        "A password cannot contain the character U+FFFE.",
        "Password");

    public static readonly Error TooShort = Error.Validation(
        "Password.TooShort",
        $"A password needs at least {PasswordPolicy.MinimumLength} characters.",
        "Password")
        .WithArgs(("min", PasswordPolicy.MinimumLength));

    public static readonly Error TooLong = Error.Validation(
        "Password.TooLong",
        $"A password must not exceed {PasswordPolicy.MaximumLength} characters.",
        "Password")
        .WithArgs(("max", PasswordPolicy.MaximumLength));

    public static readonly Error Breached = Error.Validation(
        "Password.Breached",
        "This password appears in known data breaches.",
        "Password");

    public async Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password)
    {
        if (!UnicodeText.TryNormalize(password ?? string.Empty, out var normalized))
        {
            return IdentityResult.Failed(InvalidCharacters.ToIdentityError());
        }

        var length = normalized.EnumerateRunes().Count();

        if (length < PasswordPolicy.MinimumLength)
        {
            return IdentityResult.Failed(TooShort.ToIdentityError());
        }

        if (length > PasswordPolicy.MaximumLength)
        {
            return IdentityResult.Failed(TooLong.ToIdentityError());
        }

        return await IsBreachedAsync(normalized)
            ? IdentityResult.Failed(Breached.ToIdentityError())
            : IdentityResult.Success;
    }

    private async Task<bool> IsBreachedAsync(string normalizedPassword)
    {
        try
        {
            return await pwnedPasswords.IsBreachedAsync(normalizedPassword, CancellationToken.None);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ExecutionRejectedException)
        {
            logger.LogWarning(ex, "Breached-password check unavailable. The password was accepted without it.");

            return false;
        }
    }
}

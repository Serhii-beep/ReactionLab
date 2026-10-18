using System.Collections.Frozen;
using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal static class IdentityErrors
{
    private static readonly FrozenDictionary<string, Error> ErrorsByIdentityCode = new Dictionary<string, Error>
    {
        [PasswordRulesValidator.InvalidCharacters.Code] = PasswordRulesValidator.InvalidCharacters,
        [PasswordRulesValidator.TooShort.Code] = PasswordRulesValidator.TooShort,
        [PasswordRulesValidator.TooLong.Code] = PasswordRulesValidator.TooLong,
        [PasswordRulesValidator.Breached.Code] = PasswordRulesValidator.Breached,
        [nameof(IdentityErrorDescriber.InvalidEmail)] = User.EmailInvalid
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IdentityError ToIdentityError(this Error error) =>
        new() { Code = error.Code, Description = error.Description };

    public static Error ToError(this IdentityError error) =>
        ErrorsByIdentityCode.TryGetValue(error.Code, out var known)
            ? known
            : AccountErrors.Rejected(error.Description);

    public static bool IsAlreadyRegistered(this IdentityError error) =>
        error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName);
}

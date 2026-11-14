using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record ResetPasswordRequest(
    [property: Required]
    [property: MaxLength(OneTimeLinkTokens.TokenLength)]
    string? Token,
    [property: Required]
    [property: MinLength(PasswordPolicy.MinimumLength)]
    [property: MaxLength(PasswordPolicy.MaximumLength)]
    string? NewPassword,
    [property: MaxLength(User.MaximumDisplayNameLength)]
    string? DisplayName,
    int? BirthYear,
    string? Locale,
    [property: MaxLength(User.MaximumTimeZoneLength)]
    string? TimeZone);

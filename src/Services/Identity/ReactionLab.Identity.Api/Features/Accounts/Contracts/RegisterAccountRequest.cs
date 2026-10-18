using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record RegisterAccountRequest(
    [property: Required]
    [property: MaxLength(User.MaximumEmailLength)]
    string? Email,
    [property: Required]
    [property: MinLength(PasswordPolicy.MinimumLength)]
    [property: MaxLength(PasswordPolicy.MaximumLength)]
    string? Password,
    [property: Required]
    [property: MaxLength(User.MaximumDisplayNameLength)]
    string? DisplayName,
    [property: Required]
    int? BirthYear,
    string? Locale);

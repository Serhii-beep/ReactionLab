using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Api.Passwords;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record SetPasswordRequest(
    [property: Required]
    [property: MinLength(PasswordPolicy.MinimumLength)]
    [property: MaxLength(PasswordPolicy.MaximumLength)]
    string? NewPassword);

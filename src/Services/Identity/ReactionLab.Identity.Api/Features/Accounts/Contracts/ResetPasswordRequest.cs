using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Api.Passwords;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record ResetPasswordRequest(
    [property: Required]
    [property: MaxLength(OneTimeLinkTokens.TokenLength)]
    string? Token,
    [property: Required]
    [property: MinLength(PasswordPolicy.MinimumLength)]
    [property: MaxLength(PasswordPolicy.MaximumLength)]
    string? NewPassword);

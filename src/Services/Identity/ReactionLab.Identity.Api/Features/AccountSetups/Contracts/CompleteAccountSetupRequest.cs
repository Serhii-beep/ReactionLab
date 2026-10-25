using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Api.Passwords;

namespace ReactionLab.Identity.Api.Features.AccountSetups.Contracts;

internal sealed record CompleteAccountSetupRequest(
    [property: Required]
    [property: MaxLength(AccountSetupTokens.TokenLength)]
    string? Token,
    [property: Required]
    [property: MinLength(PasswordPolicy.MinimumLength)]
    [property: MaxLength(PasswordPolicy.MaximumLength)]
    string? NewPassword);

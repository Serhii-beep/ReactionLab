using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Identity.Api.Features.AccountSetups.Contracts;

internal sealed record AccountSetupLookupRequest(
    [property: Required]
    [property: MaxLength(AccountSetupTokens.TokenLength)]
    string? Token);

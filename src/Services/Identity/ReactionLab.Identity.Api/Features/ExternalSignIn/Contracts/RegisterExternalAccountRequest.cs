using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.Contracts;

internal sealed record RegisterExternalAccountRequest(
    [property: Required]
    [property: MaxLength(User.MaximumDisplayNameLength)]
    string? DisplayName,
    [property: Required]
    int? BirthYear,
    string? Locale);

using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record FirstAdminRequest(
    [property: Required]
    [property: MaxLength(User.MaximumDisplayNameLength)]
    string? DisplayName,
    [property: Required]
    [property: MaxLength(User.MaximumUsernameLength)]
    string? Username,
    [property: MaxLength(User.MaximumEmailLength)]
    string? Email,
    [property: MaxLength(User.MaximumLocaleLength)]
    string? Locale);

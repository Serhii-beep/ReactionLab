using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record UpdateProfileRequest(
    [property: MaxLength(User.MaximumDisplayNameLength)]
    string? DisplayName,
    [property: MaxLength(User.MaximumLocaleLength)]
    string? Locale);

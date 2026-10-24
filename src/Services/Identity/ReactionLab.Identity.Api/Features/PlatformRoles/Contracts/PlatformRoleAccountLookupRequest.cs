using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.Contracts;

internal sealed record PlatformRoleAccountLookupRequest(
    [property: Required]
    [property: MaxLength(User.MaximumEmailLength)]
    string? Email);

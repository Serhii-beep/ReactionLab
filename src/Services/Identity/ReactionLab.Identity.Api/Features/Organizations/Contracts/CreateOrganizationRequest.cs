using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record CreateOrganizationRequest(
    [property: Required]
    [property: MaxLength(Organization.MaximumKindLength)]
    string? Kind,
    [property: Required]
    [property: MaxLength(Organization.MaximumNameLength)]
    string? Name,
    Guid? DistrictId,
    [property: Required]
    FirstAdminRequest? FirstAdmin);

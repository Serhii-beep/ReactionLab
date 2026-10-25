using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record OrganizationResponse(
    Guid Id,
    string Kind,
    string Name,
    Guid? DistrictId,
    string SignInCode,
    DateTimeOffset CreatedAt)
{
    public static OrganizationResponse From(Organization organization) =>
        new(
            organization.Id,
            organization.Kind,
            organization.Name,
            organization.DistrictId,
            organization.SignInCode,
            organization.CreatedAt);
}

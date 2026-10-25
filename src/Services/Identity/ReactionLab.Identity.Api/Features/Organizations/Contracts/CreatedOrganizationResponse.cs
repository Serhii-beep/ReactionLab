namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record CreatedOrganizationResponse(
    OrganizationResponse Organization,
    AccountSetupLinkResponse FirstAdmin);

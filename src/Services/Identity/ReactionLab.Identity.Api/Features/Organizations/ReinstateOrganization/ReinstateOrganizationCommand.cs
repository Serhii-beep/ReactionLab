namespace ReactionLab.Identity.Api.Features.Organizations.ReinstateOrganization;

internal sealed record ReinstateOrganizationCommand(Guid AdminId, Guid AdminSessionId, Guid OrganizationId);

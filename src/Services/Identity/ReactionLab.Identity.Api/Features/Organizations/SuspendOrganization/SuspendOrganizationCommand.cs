namespace ReactionLab.Identity.Api.Features.Organizations.SuspendOrganization;

internal sealed record SuspendOrganizationCommand(Guid AdminId, Guid AdminSessionId, Guid OrganizationId);

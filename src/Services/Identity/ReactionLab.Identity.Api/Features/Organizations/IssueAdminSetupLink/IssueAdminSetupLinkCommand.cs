namespace ReactionLab.Identity.Api.Features.Organizations.IssueAdminSetupLink;

internal sealed record IssueAdminSetupLinkCommand(
    Guid AdminId,
    Guid AdminSessionId,
    Guid OrganizationId,
    Guid AccountId);

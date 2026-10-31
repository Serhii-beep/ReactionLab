namespace ReactionLab.Identity.Api.Features.Accounts.ListAccountOrganizations;

internal sealed record ListAccountOrganizationsQuery(Guid AccountId, Guid SessionId);

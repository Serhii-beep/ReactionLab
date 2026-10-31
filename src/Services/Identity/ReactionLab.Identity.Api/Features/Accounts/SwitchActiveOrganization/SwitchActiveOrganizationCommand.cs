namespace ReactionLab.Identity.Api.Features.Accounts.SwitchActiveOrganization;

internal sealed record SwitchActiveOrganizationCommand(Guid AccountId, Guid SessionId, Guid? OrganizationId);

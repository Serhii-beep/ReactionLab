namespace ReactionLab.Identity.Api.Features.PlatformRoles.LookUpPlatformRoleAccount;

internal sealed record LookUpPlatformRoleAccountQuery(Guid AdminId, Guid AdminSessionId, string? Email);

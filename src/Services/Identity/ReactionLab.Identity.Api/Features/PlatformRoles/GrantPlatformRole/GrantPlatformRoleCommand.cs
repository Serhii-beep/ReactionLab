namespace ReactionLab.Identity.Api.Features.PlatformRoles.GrantPlatformRole;

internal sealed record GrantPlatformRoleCommand(Guid AdminId, Guid AdminSessionId, Guid AccountId, string Role);

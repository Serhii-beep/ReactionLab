namespace ReactionLab.Identity.Api.Features.PlatformRoles.RevokePlatformRole;

internal sealed record RevokePlatformRoleCommand(Guid AdminId, Guid AdminSessionId, Guid AccountId, string Role);

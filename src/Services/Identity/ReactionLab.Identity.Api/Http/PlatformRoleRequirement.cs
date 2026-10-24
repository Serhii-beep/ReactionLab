using Microsoft.AspNetCore.Authorization;

namespace ReactionLab.Identity.Api.Http;

internal sealed record PlatformRoleRequirement(string Role) : IAuthorizationRequirement;

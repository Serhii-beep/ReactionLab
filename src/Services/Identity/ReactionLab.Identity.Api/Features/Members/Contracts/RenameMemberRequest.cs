namespace ReactionLab.Identity.Api.Features.Members.Contracts;

internal sealed record RenameMemberRequest(string? DisplayName, string? Username);

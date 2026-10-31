namespace ReactionLab.Identity.Api.Features.Members.Contracts;

internal sealed record ImportMembersRequest(string? Csv, string? Credential);

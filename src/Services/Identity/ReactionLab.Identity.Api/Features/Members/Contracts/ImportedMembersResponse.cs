namespace ReactionLab.Identity.Api.Features.Members.Contracts;

internal sealed record ImportedMembersResponse(IReadOnlyList<CreatedMemberResponse> Members);

namespace ReactionLab.Identity.Api.Features.Members.EnableMember;

internal sealed record EnableMemberCommand(Guid ActorId, Guid ActorSessionId, Guid OrganizationId, Guid AccountId);

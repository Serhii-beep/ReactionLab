namespace ReactionLab.Identity.Api.Features.Members.DeleteMember;

internal sealed record DeleteMemberCommand(Guid ActorId, Guid ActorSessionId, Guid OrganizationId, Guid AccountId);

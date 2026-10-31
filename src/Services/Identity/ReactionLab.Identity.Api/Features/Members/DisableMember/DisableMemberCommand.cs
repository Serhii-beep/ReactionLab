namespace ReactionLab.Identity.Api.Features.Members.DisableMember;

internal sealed record DisableMemberCommand(Guid ActorId, Guid ActorSessionId, Guid OrganizationId, Guid AccountId);

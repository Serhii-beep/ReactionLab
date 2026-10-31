namespace ReactionLab.Identity.Api.Features.Members.EndMemberSessions;

internal sealed record EndMemberSessionsCommand(Guid ActorId, Guid ActorSessionId, Guid OrganizationId, Guid AccountId);

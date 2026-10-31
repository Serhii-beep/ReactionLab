namespace ReactionLab.Identity.Api.Features.Members.GetMember;

internal sealed record GetMemberQuery(Guid ActorId, Guid OrganizationId, Guid AccountId);

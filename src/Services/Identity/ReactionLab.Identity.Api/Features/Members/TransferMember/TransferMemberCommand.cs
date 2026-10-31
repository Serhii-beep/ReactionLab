namespace ReactionLab.Identity.Api.Features.Members.TransferMember;

internal sealed record TransferMemberCommand(
    Guid ActorId,
    Guid ActorSessionId,
    Guid OrganizationId,
    Guid AccountId,
    Guid? DestinationId);

namespace ReactionLab.Identity.Api.Features.Members.Contracts;

internal sealed record CreatedMemberResponse(MemberResponse Member, MemberCredentialResponse Credential);

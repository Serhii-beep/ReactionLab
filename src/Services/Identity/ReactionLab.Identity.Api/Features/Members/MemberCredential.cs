using ReactionLab.Identity.Api.Features.Members.Contracts;

namespace ReactionLab.Identity.Api.Features.Members;

internal sealed record MemberCredential(
    string Kind,
    string? Password,
    string? PasswordHash,
    string? SetupToken,
    byte[]? SetupTokenHash,
    DateTimeOffset IssuedAt,
    DateTimeOffset? SetupExpiresAt)
{
    public MemberCredentialResponse ToResponse() => new(Kind, Password, SetupToken, SetupExpiresAt);

    public override string ToString() => Kind;
}

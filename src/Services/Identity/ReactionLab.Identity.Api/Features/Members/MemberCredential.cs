using ReactionLab.Identity.Api.Features.Members.Contracts;

namespace ReactionLab.Identity.Api.Features.Members;

internal sealed record MemberCredential(
    string Kind,
    string? Password,
    string? PasswordHash,
    string? SetupToken,
    byte[]? SetupTokenHash,
    DateTimeOffset IssuedAt,
    DateTimeOffset? SetupExpiresAt,
    bool SetupLinkMailed)
{
    public MemberCredentialResponse ToResponse() =>
        new(Kind, Password, SetupToken, SetupExpiresAt, SetupLinkMailed);

    public override string ToString() => Kind;
}

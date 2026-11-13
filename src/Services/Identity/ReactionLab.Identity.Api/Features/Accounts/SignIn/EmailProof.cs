namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed record EmailProof(byte[]? ConfirmationLinkTokenHash, bool ByProvider)
{
    public static readonly EmailProof None = new(null, false);

    public static readonly EmailProof Provider = new(null, true);

    public static EmailProof ConfirmationLink(byte[] tokenHash) => new(tokenHash, false);
}

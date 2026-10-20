using ReactionLab.Identity.Api.ExternalSignIn;
using ReactionLab.Identity.Api.Features.ExternalSignIn.Contracts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.GetExternalIdentity;

internal sealed class GetExternalIdentityHandler(
    PendingExternalSignIns pendingSignIns,
    ExternalSignInProviders providers,
    ExternalSignInAddresses addresses) : IQueryHandler<GetExternalIdentityQuery, ExternalIdentityResponse>
{
    private const string SignUpStep = "sign-up";
    private const string LinkStep = "link";

    public async ValueTask<Result<ExternalIdentityResponse>> HandleAsync(
        GetExternalIdentityQuery query,
        CancellationToken cancellationToken)
    {
        if (await pendingSignIns.FindAsync() is not { } pending
            || providers.Find(pending.Identity.Provider) is not { } provider)
        {
            return Result.Failure<ExternalIdentityResponse>(ExternalSignInErrors.NotPending);
        }

        var address = await addresses.ResolveAsync(pending.Identity.VerifiedEmail);

        return Result.Success(new ExternalIdentityResponse(
            provider.Name,
            provider.DisplayName,
            address.Email,
            pending.Identity.Name,
            address.BelongsToPasswordAccount ? LinkStep : SignUpStep,
            pending.ReturnUrl));
    }
}

using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Http;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Sessions;

internal sealed class RecentSignInCheck(AppDbContext context, TimeProvider timeProvider)
{
    public async Task<Result> CheckAsync(
        Guid accountId,
        Guid sessionId,
        SessionSignIn signIn,
        CancellationToken cancellationToken)
    {
        if (await context.FindReauthenticationStateAsync(accountId, sessionId, cancellationToken) is not { } state)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        var now = timeProvider.GetUtcNow();

        if (!state.AccountHasPassword)
        {
            return SessionPolicy.IsRecentSignIn(signIn.SignedInAt, now)
                ? Result.Success()
                : Result.Failure(SessionErrors.RecentSignInRequired);
        }

        var passwordSignedInAt = signIn.WithPassword ? signIn.SignedInAt : null;
        var hasRecentPassword = SessionPolicy.IsRecentSignIn(passwordSignedInAt, now)
            || SessionPolicy.IsRecentSignIn(state.PasswordVerifiedAt, now);

        return hasRecentPassword ? Result.Success() : Result.Failure(SessionErrors.RecentPasswordRequired);
    }
}

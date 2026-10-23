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
        var state = await FindValidStateAsync(accountId, sessionId, cancellationToken);

        if (state.IsFailure)
        {
            return Result.Failure(state.Error);
        }

        var now = timeProvider.GetUtcNow();

        if (!HasPasswordPastProbation(state.Value, now))
        {
            return CheckRecentSignIn(signIn, now);
        }

        var passwordSignedInAt = signIn.WithPassword ? signIn.SignedInAt : null;
        var hasRecentPassword = SessionPolicy.IsRecentSignIn(passwordSignedInAt, now)
            || SessionPolicy.IsRecentSignIn(state.Value.PasswordVerifiedAt, now);

        return hasRecentPassword ? Result.Success() : Result.Failure(SessionErrors.RecentPasswordRequired);
    }

    public async Task<Result> CheckSignInMethodChangeAsync(
        Guid accountId,
        Guid sessionId,
        SessionSignIn signIn,
        CancellationToken cancellationToken)
    {
        var state = await FindValidStateAsync(accountId, sessionId, cancellationToken);

        if (state.IsFailure)
        {
            return Result.Failure(state.Error);
        }

        var now = timeProvider.GetUtcNow();

        return HasPasswordPastProbation(state.Value, now)
            ? CheckConfirmation(state.Value, now)
            : CheckRecentSignIn(signIn, now);
    }

    public async Task<Result> CheckConfirmedPasswordAsync(
        Guid accountId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var state = await FindValidStateAsync(accountId, sessionId, cancellationToken);

        if (state.IsFailure)
        {
            return Result.Failure(state.Error);
        }

        if (!state.Value.AccountHasPassword)
        {
            return Result.Failure(AccountErrors.PasswordNotSet);
        }

        var now = timeProvider.GetUtcNow();

        if (state.Value.PasswordProbationEndsAt is { } probationEndsAt && probationEndsAt > now)
        {
            return Result.Failure(AccountErrors.PasswordTooNew(probationEndsAt));
        }

        return CheckConfirmation(state.Value, now);
    }

    private async Task<Result<ReauthenticationState>> FindValidStateAsync(
        Guid accountId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (await context.FindReauthenticationStateAsync(accountId, sessionId, cancellationToken) is not { } state)
        {
            return Result.Failure<ReauthenticationState>(AccountErrors.NotFound);
        }

        return state.SessionValid ? Result.Success(state) : Result.Failure<ReauthenticationState>(SessionErrors.Ended);
    }

    private static bool HasPasswordPastProbation(ReauthenticationState state, DateTimeOffset now) =>
        state.AccountHasPassword && !(state.PasswordProbationEndsAt > now);

    private static Result CheckRecentSignIn(SessionSignIn signIn, DateTimeOffset now) =>
        SessionPolicy.IsRecentSignIn(signIn.SignedInAt, now)
            ? Result.Success()
            : Result.Failure(SessionErrors.RecentSignInRequired);

    private static Result CheckConfirmation(ReauthenticationState state, DateTimeOffset now) =>
        SessionPolicy.IsRecentSignIn(state.PasswordVerifiedAt, now)
            ? Result.Success()
            : Result.Failure(SessionErrors.PasswordConfirmationRequired);
}

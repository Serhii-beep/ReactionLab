using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed class SignInAttempts(AppDbContext context, TimeProvider timeProvider, SignInMetrics metrics)
{
    public long BeginAttempt() => timeProvider.GetTimestamp();

    public Task<bool> ClaimAsync(Guid userId) =>
        context.ClaimSignInAttemptAsync(userId, PasswordPolicy.Lockout, timeProvider.GetUtcNow());

    public Task RecordSuccessAsync(Guid userId) =>
        context.ResetSignInAttemptsAsync(userId);

    public Task ReplacePasswordHashAsync(Guid userId, string previousHash, string replacementHash) =>
        context.ReplacePasswordHashAsync(userId, previousHash, replacementHash);

    public async Task<Result> FailAsync(long beganAt, CancellationToken cancellationToken)
    {
        var remaining = PasswordPolicy.MinimumFailedSignInDuration - timeProvider.GetElapsedTime(beganAt);

        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, timeProvider, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
        else
        {
            metrics.RecordSlowFailure();
        }

        return Result.Failure(AccountErrors.SignInFailed);
    }
}

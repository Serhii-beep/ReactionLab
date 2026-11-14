using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Messaging.Outbox;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed class SignInAttempts(
    AppDbContext context,
    OutboxWriter outbox,
    TimeProvider timeProvider,
    SignInMetrics metrics)
{
    public long BeginAttempt() => timeProvider.GetTimestamp();

    public Task<bool> ClaimAsync(User account) =>
        context.ClaimSignInAttemptAsync(
            account.Id,
            PasswordPolicy.Lockout,
            timeProvider.GetUtcNow(),
            LockAnnouncements.For(outbox, account, AccountLocks.SignIn));

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

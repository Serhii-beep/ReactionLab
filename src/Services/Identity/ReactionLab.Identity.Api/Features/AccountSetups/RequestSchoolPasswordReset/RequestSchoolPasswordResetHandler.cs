using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.Messaging.Outbox;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.AccountSetups.RequestSchoolPasswordReset;

internal sealed class RequestSchoolPasswordResetHandler(
    AppDbContext context,
    OutboxWriter outbox,
    TimeProvider timeProvider,
    ILogger<RequestSchoolPasswordResetHandler> logger) : ICommandHandler<RequestSchoolPasswordResetCommand>
{
    private static readonly TimeSpan MinimumAnswerDuration = TimeSpan.FromMilliseconds(250);

    public async ValueTask<Result> HandleAsync(
        RequestSchoolPasswordResetCommand command,
        CancellationToken cancellationToken)
    {
        var beganAt = timeProvider.GetTimestamp();
        var signInCode = Organization.NormalizeSignInCode(command.SignInCode);
        var username = User.NormalizeOrganizationUsername(command.Username);

        if (new Result[] { signInCode, username }.FirstOrDefault(result => result.IsFailure) is { } invalid)
        {
            return invalid;
        }

        var account = await context.FindSchoolAccountAsync(signInCode.Value, username.Value, cancellationToken);

        if (account is not null)
        {
            var requested = new AccountSetupRequested(
                account.Id,
                account.Version,
                Guid.CreateVersion7(),
                AccountSetupPurposes.SelfReset);

            if (await context.RequestSelfResetAsync(
                account.Id,
                requested.RequestId,
                outbox.CreateMessage(requested)) > 0)
            {
                logger.LogInformation("School account {AccountId} asked for a password reset link.", account.Id);
            }
        }

        var remaining = MinimumAnswerDuration - timeProvider.GetElapsedTime(beganAt);

        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, timeProvider, cancellationToken)
                .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        return Result.Success();
    }
}

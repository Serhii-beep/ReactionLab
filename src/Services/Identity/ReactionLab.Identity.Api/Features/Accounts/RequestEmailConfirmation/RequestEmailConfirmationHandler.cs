using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.SecurityLinks;
using ReactionLab.Messaging.Outbox;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.RequestEmailConfirmation;

internal sealed class RequestEmailConfirmationHandler(
    UserManager<User> userManager,
    AppDbContext context,
    OutboxWriter outbox,
    TimeProvider timeProvider,
    ILogger<RequestEmailConfirmationHandler> logger) : ICommandHandler<RequestEmailConfirmationCommand>
{
    private static readonly TimeSpan MinimumAnswerDuration = TimeSpan.FromMilliseconds(250);

    public async ValueTask<Result> HandleAsync(
        RequestEmailConfirmationCommand command,
        CancellationToken cancellationToken)
    {
        var beganAt = timeProvider.GetTimestamp();
        var address = User.NormalizeEmail(command.Email);

        if (address.IsFailure)
        {
            return Result.Failure(address.Error);
        }

        var account = await userManager.FindByEmailAsync(address.Value);

        if (account is { EmailConfirmed: false, EmailConfirmationExempt: false, PasswordHash: not null })
        {
            var requested = new EmailConfirmationRequested(account.Id, account.Version, Guid.CreateVersion7());

            if (await context.RequestEmailConfirmationAsync(
                account.Id,
                requested.RequestId,
                outbox.CreateMessage(requested)) > 0)
            {
                logger.LogInformation("Account {AccountId} asked for a new confirmation link.", account.Id);
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

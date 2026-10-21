using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.Reauthenticate;

internal sealed class ReauthenticateHandler(
    UserManager<User> userManager,
    CurrentPasswordCheck currentPasswordCheck,
    ILogger<ReauthenticateHandler> logger) : ICommandHandler<ReauthenticateCommand>
{
    public async ValueTask<Result> HandleAsync(ReauthenticateCommand command, CancellationToken cancellationToken)
    {
        if (await userManager.FindByIdAsync(command.AccountId.ToString()) is not { } user)
        {
            return Result.Failure(AccountErrors.NotFound);
        }

        var verifiedHash = await currentPasswordCheck.GetVerifiedHashAsync(
            user,
            command.CurrentSessionId,
            command.SignIn.WithPassword,
            command.CurrentPassword,
            cancellationToken);

        if (verifiedHash.IsFailure)
        {
            return Result.Failure(verifiedHash.Error);
        }

        logger.LogInformation(
            "Session {SessionId} confirmed the password of account {AccountId}.",
            command.CurrentSessionId,
            command.AccountId);

        return Result.Success();
    }
}

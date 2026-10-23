using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Api.OpenId;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.ChangePassword;

internal sealed class ChangePasswordHandler(
    UserManager<User> userManager,
    CurrentPasswordCheck currentPasswordCheck,
    NewPasswordCheck newPasswordCheck,
    SecurityStampRotation securityStampRotation,
    ILogger<ChangePasswordHandler> logger) : ICommandHandler<ChangePasswordCommand>
{
    public async ValueTask<Result> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken)
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

        var replacementHash = await newPasswordCheck.GetValidHashAsync(user, command.NewPassword, cancellationToken);

        if (replacementHash.IsFailure)
        {
            return Result.Failure(replacementHash.Error);
        }

        var rotation = await securityStampRotation.ReplacePasswordAsync(
            command.AccountId,
            command.CurrentSessionId,
            verifiedHash.Value,
            replacementHash.Value);

        if (!rotation.AccountMatched)
        {
            logger.LogInformation(
                "Session {SessionId} lost its password change on account {AccountId} to another change.",
                command.CurrentSessionId,
                command.AccountId);

            return Result.Failure(AccountErrors.CurrentPasswordIncorrect);
        }

        if (!rotation.ActingSessionValid)
        {
            logger.LogInformation(
                "Session {SessionId} ended during its password change on account {AccountId}. Nothing changed.",
                command.CurrentSessionId,
                command.AccountId);

            return Result.Failure(SessionErrors.Ended);
        }

        logger.LogInformation(
            "Session {SessionId} changed the password of account {AccountId}, ending {SessionCount} other session(s).",
            command.CurrentSessionId,
            command.AccountId,
            rotation.EndedSessionCount);

        return Result.Success();
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed class SignInHandler(
    PasswordSignInVerifier passwordVerifier,
    SignInManager<User> signInManager) : ICommandHandler<SignInCommand>
{
    public async ValueTask<Result> HandleAsync(SignInCommand command, CancellationToken cancellationToken)
    {
        var user = await passwordVerifier.VerifyAsync(command.Email, command.Password, cancellationToken);

        if (user.IsFailure)
        {
            return Result.Failure(user.Error);
        }

        await signInManager.SignInWithClaimsAsync(
            user.Value,
            isPersistent: false,
            [new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Password)]);

        return Result.Success();
    }
}

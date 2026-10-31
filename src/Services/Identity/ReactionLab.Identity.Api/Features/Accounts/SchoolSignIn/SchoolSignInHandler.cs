using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.Accounts.SignIn;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SchoolSignIn;

internal sealed class SchoolSignInHandler(
    PasswordSignInVerifier passwordVerifier,
    SignInManager<User> signInManager) : ICommandHandler<SchoolSignInCommand>
{
    public async ValueTask<Result> HandleAsync(SchoolSignInCommand command, CancellationToken cancellationToken)
    {
        var user = await passwordVerifier.VerifySchoolAccountAsync(
            command.SignInCode,
            command.Username,
            command.Password,
            cancellationToken);

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

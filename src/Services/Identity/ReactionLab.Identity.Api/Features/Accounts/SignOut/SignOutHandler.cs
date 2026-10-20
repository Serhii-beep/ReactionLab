using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SignOut;

internal sealed class SignOutHandler(SignInManager<User> signInManager) : ICommandHandler<SignOutCommand>
{
    public async ValueTask<Result> HandleAsync(SignOutCommand command, CancellationToken cancellationToken)
    {
        var session = await signInManager.Context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var pendingSignIn = await signInManager.Context.AuthenticateAsync(IdentityConstants.ExternalScheme);

        if (session.Succeeded || pendingSignIn.Succeeded)
        {
            await signInManager.SignOutAsync();
        }

        return Result.Success();
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed class SignInHandler(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    PasswordVerificationGate verificationGate,
    SignInAttempts signInAttempts) : ICommandHandler<SignInCommand>
{
    public async ValueTask<Result> HandleAsync(SignInCommand command, CancellationToken cancellationToken)
    {
        var beganAt = signInAttempts.BeginAttempt();

        if (!UnicodeText.TryNormalize(command.Password ?? string.Empty, out var password))
        {
            return await RefuseAsync(string.Empty, beganAt, cancellationToken);
        }

        var user = await FindUserAsync(command.Email);

        if (user?.PasswordHash is not { } passwordHash
            || !await signInManager.CanSignInAsync(user)
            || !await signInAttempts.ClaimAsync(user.Id))
        {
            return await RefuseAsync(password, beganAt, cancellationToken);
        }

        var (verified, replacementHash) = await verificationGate.RunAsync(
            () => CheckPassword(user, passwordHash, password),
            cancellationToken);

        if (!verified)
        {
            return await signInAttempts.FailAsync(beganAt, cancellationToken);
        }

        await signInAttempts.RecordSuccessAsync(user.Id);

        if (replacementHash is not null)
        {
            await signInAttempts.ReplacePasswordHashAsync(user.Id, passwordHash, replacementHash);
        }

        await signInManager.SignInWithClaimsAsync(
            user,
            isPersistent: false,
            [new Claim(AuthenticationMethods.ClaimType, AuthenticationMethods.Password)]);

        return Result.Success();
    }

    private async Task<User?> FindUserAsync(string? email)
    {
        var address = User.NormalizeEmail(email);

        return address.IsSuccess ? await userManager.FindByEmailAsync(address.Value) : null;
    }

    private (bool Verified, string? ReplacementHash) CheckPassword(User user, string passwordHash, string password) =>
        userManager.PasswordHasher.VerifyHashedPassword(user, passwordHash, password) switch
        {
            PasswordVerificationResult.Failed => (false, null),
            PasswordVerificationResult.SuccessRehashNeeded => (true, userManager.PasswordHasher.HashPassword(user, password)),
            _ => (true, null)
        };

    private async Task<Result> RefuseAsync(string password, long beganAt, CancellationToken cancellationToken)
    {
        await verificationGate.VerifyDecoyAsync(password, cancellationToken);

        return await signInAttempts.FailAsync(beganAt, cancellationToken);
    }
}

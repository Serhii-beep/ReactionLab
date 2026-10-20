using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed class PasswordSignInVerifier(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    PasswordVerificationGate verificationGate,
    SignInAttempts signInAttempts)
{
    public async Task<Result<User>> VerifyAsync(string? email, string? password, CancellationToken cancellationToken)
    {
        var beganAt = signInAttempts.BeginAttempt();

        if (!UnicodeText.TryNormalize(password ?? string.Empty, out var normalizedPassword))
        {
            return await RefuseAsync(string.Empty, beganAt, cancellationToken);
        }

        var user = await FindUserAsync(email);

        if (user?.PasswordHash is not { } passwordHash
            || !await signInManager.CanSignInAsync(user)
            || !await signInAttempts.ClaimAsync(user.Id))
        {
            return await RefuseAsync(normalizedPassword, beganAt, cancellationToken);
        }

        var (verified, replacementHash) = await verificationGate.RunAsync(
            () => CheckPassword(user, passwordHash, normalizedPassword),
            cancellationToken);

        if (!verified)
        {
            return await FailAsync(beganAt, cancellationToken);
        }

        await signInAttempts.RecordSuccessAsync(user.Id);

        if (replacementHash is not null)
        {
            await signInAttempts.ReplacePasswordHashAsync(user.Id, passwordHash, replacementHash);
        }

        return user;
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
            PasswordVerificationResult.SuccessRehashNeeded =>
                (true, userManager.PasswordHasher.HashPassword(user, password)),
            _ => (true, null)
        };

    private async Task<Result<User>> RefuseAsync(string password, long beganAt, CancellationToken cancellationToken)
    {
        await verificationGate.VerifyDecoyAsync(password, cancellationToken);

        return await FailAsync(beganAt, cancellationToken);
    }

    private async Task<Result<User>> FailAsync(long beganAt, CancellationToken cancellationToken) =>
        Result.Failure<User>((await signInAttempts.FailAsync(beganAt, cancellationToken)).Error);
}

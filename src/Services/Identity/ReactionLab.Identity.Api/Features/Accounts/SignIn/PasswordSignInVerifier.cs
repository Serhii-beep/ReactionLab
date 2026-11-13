using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.Identity.Persistence.SecurityLinks;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed class PasswordSignInVerifier(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    PasswordVerificationGate verificationGate,
    SignInAttempts signInAttempts,
    AppDbContext context)
{
    public Task<Result<User>> VerifyAsync(
        string? email,
        string? password,
        string? emailConfirmationToken,
        CancellationToken cancellationToken) =>
        VerifyPasswordAsync(
            () => FindUserAsync(email),
            password,
            OneTimeLinkTokens.TryHash(emailConfirmationToken, out var tokenHash)
                ? EmailProof.ConfirmationLink(tokenHash)
                : EmailProof.None,
            cancellationToken);

    public Task<Result<User>> VerifyWithProvenEmailAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken) =>
        VerifyPasswordAsync(() => FindUserAsync(email), password, EmailProof.Provider, cancellationToken);

    public Task<Result<User>> VerifySchoolAccountAsync(
        string? signInCode,
        string? username,
        string? password,
        CancellationToken cancellationToken) =>
        VerifyPasswordAsync(
            () => FindSchoolAccountAsync(signInCode, username, cancellationToken),
            password,
            EmailProof.None,
            cancellationToken);

    private async Task<Result<User>> VerifyPasswordAsync(
        Func<Task<User?>> findAccount,
        string? password,
        EmailProof emailProof,
        CancellationToken cancellationToken)
    {
        var beganAt = signInAttempts.BeginAttempt();

        if (!UnicodeText.TryNormalize(password ?? string.Empty, out var normalizedPassword))
        {
            return await RefuseAsync(string.Empty, beganAt, cancellationToken);
        }

        var user = await findAccount();

        if (user?.PasswordHash is not { } passwordHash
            || !await MaySignInAsync(user, emailProof, cancellationToken)
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

        if (emailProof != EmailProof.None
            && !user.EmailConfirmed
            && !await ConfirmEmailAsync(user, emailProof, cancellationToken))
        {
            return await FailAsync(beganAt, cancellationToken);
        }

        return user;
    }

    private async Task<bool> MaySignInAsync(User user, EmailProof emailProof, CancellationToken cancellationToken) =>
        await signInManager.CanSignInAsync(user)
        || emailProof.ByProvider
        || emailProof.ConfirmationLinkTokenHash is { } tokenHash
            && await context.HoldsEmailConfirmationLinkAsync(user.Id, tokenHash, cancellationToken);

    private async Task<bool> ConfirmEmailAsync(User user, EmailProof emailProof, CancellationToken cancellationToken)
    {
        if (emailProof.ByProvider)
        {
            await context.ConfirmEmailThroughProviderAsync(user.Id, user.Email ?? string.Empty);

            return true;
        }

        if (emailProof.ConfirmationLinkTokenHash is { } tokenHash
            && await context.ConfirmEmailWithLinkAsync(user.Id, tokenHash) > 0)
        {
            return true;
        }

        return await signInManager.CanSignInAsync(user)
            || await context.FindContactAsync(user.Id, cancellationToken) is { EmailConfirmed: true };
    }

    private async Task<User?> FindUserAsync(string? email)
    {
        var address = User.NormalizeEmail(email);

        return address.IsSuccess ? await userManager.FindByEmailAsync(address.Value) : null;
    }

    private async Task<User?> FindSchoolAccountAsync(
        string? signInCode,
        string? username,
        CancellationToken cancellationToken)
    {
        var normalizedSignInCode = Organization.NormalizeSignInCode(signInCode);
        var normalizedUsername = User.NormalizeOrganizationUsername(username);

        return normalizedSignInCode.IsSuccess && normalizedUsername.IsSuccess
            ? await context.FindSchoolAccountAsync(
                normalizedSignInCode.Value,
                normalizedUsername.Value,
                cancellationToken)
            : null;
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

using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal sealed class NewPasswordCheck(UserManager<User> userManager, PasswordVerificationGate verificationGate)
{
    private const string NewPasswordField = "NewPassword";

    public async Task<Result<string>> GetValidHashAsync(
        User user,
        string? newPassword,
        CancellationToken cancellationToken)
    {
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, user, newPassword);

            if (!validation.Succeeded)
            {
                return validation.Errors.First().ToError() with { Field = NewPasswordField };
            }
        }

        var password = newPassword ?? string.Empty;

        return await verificationGate.RunAsync(
            () => userManager.PasswordHasher.HashPassword(user, password),
            cancellationToken);
    }
}

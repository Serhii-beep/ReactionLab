using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn;

internal sealed class ExternalSignInAddresses(UserManager<User> userManager)
{
    public async Task<ExternalSignInAddress> ResolveAsync(string? verifiedEmail)
    {
        if (verifiedEmail is null || await userManager.FindByEmailAsync(verifiedEmail) is not { } holder)
        {
            return new ExternalSignInAddress(verifiedEmail, BelongsToPasswordAccount: false, CanBeTakenOver: false);
        }

        return holder.PasswordHash is null
            ? new ExternalSignInAddress(null, BelongsToPasswordAccount: false, CanBeTakenOver: false)
            : new ExternalSignInAddress(
                verifiedEmail,
                BelongsToPasswordAccount: true,
                CanBeTakenOver: holder is { EmailConfirmed: false, OrganizationId: null });
    }
}

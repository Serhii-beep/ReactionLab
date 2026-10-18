using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Passwords;

internal sealed class NormalizingPasswordHasher(IOptions<PasswordHasherOptions> options) : IPasswordHasher<User>
{
    private readonly PasswordHasher<User> _passwordHasher = new(options);

    public string HashPassword(User user, string password) =>
        _passwordHasher.HashPassword(user, password.Normalize(NormalizationForm.FormC));

    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword) =>
        UnicodeText.TryNormalize(providedPassword, out var normalized)
            ? _passwordHasher.VerifyHashedPassword(user, hashedPassword, normalized)
            : PasswordVerificationResult.Failed;
}

using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.AccountSetups;
using ReactionLab.Identity.Api.Passwords;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members;

internal sealed class MemberCredentials(
    UserManager<User> userManager,
    PasswordVerificationGate verificationGate,
    AppDbContext context,
    TimeProvider timeProvider)
{
    public static Result<string> ReadKind(string? kind) =>
        kind is MemberCredentialKinds.Password or MemberCredentialKinds.SetupLink
            ? kind
            : MemberErrors.CredentialUnknown;

    public async Task<MemberCredential> PrepareAsync(User account, string kind, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetDatabaseUtcNow();

        if (kind == MemberCredentialKinds.SetupLink)
        {
            var (setupToken, setupTokenHash) = AccountSetupTokens.Issue();

            return new MemberCredential(kind, null, null, setupToken, setupTokenHash, now, now + AccountSetup.Lifetime);
        }

        var password = GeneratedPasswords.New();
        var passwordHash = await verificationGate.RunAsync(
            () => userManager.PasswordHasher.HashPassword(account, password),
            cancellationToken);

        return new MemberCredential(kind, password, passwordHash, null, null, now, null);
    }

    public async Task<bool> ApplyAsync(
        MemberCredential credential,
        Guid accountId,
        Guid organizationId,
        Guid issuedBy,
        Guid issuedInSession)
    {
        if (credential.PasswordHash is { } passwordHash)
        {
            return await context.SetMemberPasswordAsync(accountId, organizationId, passwordHash) == 1;
        }

        return await context.ClearMemberPasswordAsync(accountId, organizationId) == 1
            && await context.IssueAccountSetupAsync(
                organizationId,
                accountId,
                credential.SetupTokenHash!,
                credential.IssuedAt,
                credential.SetupExpiresAt!.Value,
                issuedBy,
                issuedInSession) == 1;
    }
}

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
    MailedAccountSetups mailedSetups,
    TimeProvider timeProvider)
{
    public static Result<string> ReadKind(string? kind) =>
        kind is MemberCredentialKinds.Password or MemberCredentialKinds.SetupLink
            ? kind
            : MemberErrors.CredentialUnknown;

    public async Task<MemberCredential> PrepareAsync(User account, string kind, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetDatabaseUtcNow();

        if (kind == MemberCredentialKinds.SetupLink && account.Email is not null)
        {
            return new MemberCredential(kind, null, null, null, null, now, now + AccountSetup.Lifetime, true);
        }

        if (kind == MemberCredentialKinds.SetupLink)
        {
            var (setupToken, setupTokenHash) = OneTimeLinkTokens.Issue();

            return new MemberCredential(
                kind,
                null,
                null,
                setupToken,
                setupTokenHash,
                now,
                now + AccountSetup.Lifetime,
                false);
        }

        var password = GeneratedPasswords.New();
        var passwordHash = await verificationGate.RunAsync(
            () => userManager.PasswordHasher.HashPassword(account, password),
            cancellationToken);

        return new MemberCredential(kind, password, passwordHash, null, null, now, null, false);
    }

    public async Task<Result> ApplyAsync(
        MemberCredential credential,
        Guid accountId,
        Guid organizationId,
        Guid issuedBy,
        Guid issuedInSession)
    {
        if (credential.PasswordHash is { } passwordHash)
        {
            return Applied(await context.SetMemberPasswordAsync(accountId, organizationId, passwordHash));
        }

        if (credential.SetupLinkMailed
            && !await mailedSetups.RequestAsync(organizationId, accountId, issuedBy, issuedInSession))
        {
            return Result.Failure(MemberErrors.SetupLinkLimitReached);
        }

        if (await context.ClearMemberPasswordAsync(accountId, organizationId) != 1)
        {
            return Result.Failure(MemberErrors.NotFound);
        }

        return credential.SetupLinkMailed
            ? Result.Success()
            : Applied(await context.IssueAccountSetupAsync(
                organizationId,
                accountId,
                credential.SetupTokenHash!,
                credential.IssuedAt,
                credential.SetupExpiresAt!.Value,
                issuedBy,
                issuedInSession));
    }

    public static string ChangeName(MemberCredential credential) =>
        credential switch
        {
            { SetupLinkMailed: true } => OrganizationChangeNames.SetupLinkMailed,
            { Kind: MemberCredentialKinds.SetupLink } => OrganizationChangeNames.SetupLinkIssued,
            _ => OrganizationChangeNames.PasswordReset
        };

    private static Result Applied(int rows) => rows == 1 ? Result.Success() : Result.Failure(MemberErrors.NotFound);
}

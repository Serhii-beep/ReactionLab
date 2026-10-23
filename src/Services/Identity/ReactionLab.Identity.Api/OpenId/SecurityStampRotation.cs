using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.OpenId;

namespace ReactionLab.Identity.Api.OpenId;

internal sealed class SecurityStampRotation(AppDbContext context)
{
    private const int SecurityStampLength = 32;

    public Task<SecurityStampRotationOutcome> RotateAsync(Guid accountId, Guid currentSessionId) =>
        RunAsync(accountId, currentSessionId, securityStamp =>
            context.ReplaceSecurityStampAsync(accountId, securityStamp));

    public Task<SecurityStampRotationOutcome> ReplacePasswordAsync(
        Guid accountId,
        Guid currentSessionId,
        string previousHash,
        string replacementHash) =>
        RunAsync(accountId, currentSessionId, securityStamp =>
            context.ReplacePasswordAndSecurityStampAsync(accountId, previousHash, replacementHash, securityStamp));

    public Task<SecurityStampRotationOutcome> RemoveLoginAsync(
        Guid accountId,
        Guid currentSessionId,
        string provider,
        string[] usableProviders,
        DateTimeOffset now) =>
        RunAsync(accountId, currentSessionId, securityStamp =>
            context.RemoveLoginAndReplaceSecurityStampAsync(accountId, provider, usableProviders, now, securityStamp));

    public Task<SecurityStampRotationOutcome> RemovePasswordAsync(
        Guid accountId,
        Guid currentSessionId,
        string[] usableProviders) =>
        RunAsync(accountId, currentSessionId, securityStamp =>
            context.RemovePasswordAndReplaceSecurityStampAsync(accountId, usableProviders, securityStamp));

    private Task<SecurityStampRotationOutcome> RunAsync(
        Guid accountId,
        Guid currentSessionId,
        Func<string, Task<string?>> replaceSecurityStamp) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            var subject = accountId.ToString();
            var securityStamp = RandomNumberGenerator.GetHexString(SecurityStampLength);

            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            if (await replaceSecurityStamp(securityStamp) is not { } previousStamp)
            {
                return SecurityStampRotationOutcome.AccountUnmatched;
            }

            var revocation = await context.RevokeOtherSessionsAsync(subject, currentSessionId);

            if (!revocation.ActingSessionValid
                || await context.KeepSessionThroughStampChangeAsync(
                    subject,
                    currentSessionId,
                    SessionSecurityStamps.Hash(previousStamp),
                    SessionSecurityStamps.Hash(securityStamp)) == 0)
            {
                return SecurityStampRotationOutcome.ActingSessionEnded;
            }

            await transaction.CommitAsync(CancellationToken.None);

            return SecurityStampRotationOutcome.Rotated(revocation.EndedSessionCount);
        });
}

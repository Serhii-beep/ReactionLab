using System.Data;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.PlatformRoles;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.PlatformRoles;

internal sealed class PlatformAdminTransaction(AppDbContext context, RecentSignInCheck recentSignInCheck)
{
    public Task<Result<TOutcome>> RunAsync<TOutcome>(
        Guid adminId,
        Guid adminSessionId,
        Func<Task<Result<TOutcome>>> change) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                CancellationToken.None);

            await context.LockPlatformRoleChangesAsync();

            if (!await context.HoldsPlatformRoleAsync(adminId, PlatformRoleNames.Admin, CancellationToken.None))
            {
                return Result.Failure<TOutcome>(PlatformRoleErrors.AdminRequired);
            }

            if (!await context.LockValidSessionAsync(adminId.ToString(), adminSessionId))
            {
                return Result.Failure<TOutcome>(SessionErrors.Ended);
            }

            var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
                adminId,
                adminSessionId,
                CancellationToken.None);

            if (proof.IsFailure)
            {
                return Result.Failure<TOutcome>(proof.Error);
            }

            var outcome = await change();

            if (outcome.IsSuccess)
            {
                await transaction.CommitAsync(CancellationToken.None);
            }

            return outcome;
        });
}

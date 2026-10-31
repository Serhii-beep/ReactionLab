using System.Data;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Organizations;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.OpenId;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members;

internal sealed class SchoolAdminTransaction(AppDbContext context, RecentSignInCheck recentSignInCheck)
{
    public async Task<Result<OrganizationAccess>> AuthorizeAsync(
        Guid actorId,
        Guid organizationId,
        Func<OrganizationAccess, Error?> rule,
        CancellationToken cancellationToken)
    {
        if (await context.FindOrganizationAccessAsync(actorId, organizationId, cancellationToken) is not { } access)
        {
            return OrganizationErrors.NotFound;
        }

        return rule(access) is { } refusal ? refusal : access;
    }

    public async Task<Result<OrganizationAccess>> CheckAsync(
        Guid actorId,
        Guid actorSessionId,
        Guid organizationId,
        Func<OrganizationAccess, Error?> rule,
        CancellationToken cancellationToken)
    {
        var access = await AuthorizeAsync(actorId, organizationId, rule, cancellationToken);

        if (access.IsFailure)
        {
            return access;
        }

        var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(actorId, actorSessionId, cancellationToken);

        return proof.IsFailure ? proof.Error : access;
    }

    public Task<Result<TOutcome>> RunAsync<TOutcome>(
        Guid actorId,
        Guid actorSessionId,
        Guid organizationId,
        Func<OrganizationAccess, Error?> rule,
        IReadOnlyCollection<Guid> alsoLockedOrganizationIds,
        Guid? changedAccountId,
        Func<OrganizationAccess, Task<Result<TOutcome>>> change) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                CancellationToken.None);

            await context.LockOrganizationsAsync([organizationId, .. alsoLockedOrganizationIds]);

            if (changedAccountId is { } accountId)
            {
                await context.LockMemberAsync(accountId);
            }

            var access = await AuthorizeAsync(actorId, organizationId, rule, CancellationToken.None);

            if (access.IsFailure)
            {
                return Result.Failure<TOutcome>(access.Error);
            }

            if (!await context.LockValidSessionAsync(actorId.ToString(), actorSessionId))
            {
                return Result.Failure<TOutcome>(SessionErrors.Ended);
            }

            var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
                actorId,
                actorSessionId,
                CancellationToken.None);

            if (proof.IsFailure)
            {
                return Result.Failure<TOutcome>(proof.Error);
            }

            var outcome = await change(access.Value);

            if (outcome.IsSuccess)
            {
                await transaction.CommitAsync(CancellationToken.None);
            }

            return outcome;
        });

    public async Task<Result<TOutcome>> RunLockingMemberAsync<TOutcome>(
        Guid actorId,
        Guid actorSessionId,
        Guid organizationId,
        Guid accountId,
        Func<OrganizationAccess, Error?> rule,
        Func<OrganizationAccess, Task<Result<TOutcome>>> change,
        CancellationToken cancellationToken)
    {
        var outcome = await RunLockingAdminOrganizationsAsync(
            actorId,
            actorSessionId,
            organizationId,
            accountId,
            rule,
            change,
            cancellationToken);

        if (outcome.IsSuccess || outcome.Error != MemberErrors.RolesChanged)
        {
            return outcome;
        }

        return await RunLockingAdminOrganizationsAsync(
            actorId,
            actorSessionId,
            organizationId,
            accountId,
            rule,
            change,
            cancellationToken);
    }

    private async Task<Result<TOutcome>> RunLockingAdminOrganizationsAsync<TOutcome>(
        Guid actorId,
        Guid actorSessionId,
        Guid organizationId,
        Guid accountId,
        Func<OrganizationAccess, Error?> rule,
        Func<OrganizationAccess, Task<Result<TOutcome>>> change,
        CancellationToken cancellationToken)
    {
        var adminOrganizationIds = await context.ListAdminOrganizationIdsAsync(accountId, cancellationToken);

        return await RunAsync(
            actorId,
            actorSessionId,
            organizationId,
            rule,
            adminOrganizationIds,
            accountId,
            async lockedAccess =>
            {
                var lockedAdminOrganizationIds = await context.ListAdminOrganizationIdsAsync(
                    accountId,
                    CancellationToken.None);

                if (lockedAdminOrganizationIds.Except([organizationId, .. adminOrganizationIds]).Any())
                {
                    return MemberErrors.RolesChanged;
                }

                return await change(lockedAccess);
            });
    }
}

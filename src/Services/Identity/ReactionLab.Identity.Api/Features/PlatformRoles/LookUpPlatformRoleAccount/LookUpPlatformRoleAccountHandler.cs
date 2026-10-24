using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.PlatformRoles.Contracts;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.PlatformRoles;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.LookUpPlatformRoleAccount;

internal sealed class LookUpPlatformRoleAccountHandler(
    RecentSignInCheck recentSignInCheck,
    UserManager<User> userManager,
    AppDbContext context,
    ILogger<LookUpPlatformRoleAccountHandler> logger)
    : IQueryHandler<LookUpPlatformRoleAccountQuery, PlatformRoleAccountResponse>
{
    public async ValueTask<Result<PlatformRoleAccountResponse>> HandleAsync(
        LookUpPlatformRoleAccountQuery query,
        CancellationToken cancellationToken)
    {
        var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
            query.AdminId,
            query.AdminSessionId,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not look up an account: {ErrorCode}.",
                query.AdminSessionId,
                proof.Error.Code);

            return Result.Failure<PlatformRoleAccountResponse>(proof.Error);
        }

        var address = User.NormalizeEmail(query.Email);

        if (address.IsFailure)
        {
            return Result.Failure<PlatformRoleAccountResponse>(address.Error);
        }

        if (await userManager.FindByEmailAsync(address.Value) is not { } user)
        {
            logger.LogInformation(
                "Admin {AdminId} looked up an address in session {SessionId} and found no account.",
                query.AdminId,
                query.AdminSessionId);

            return Result.Failure<PlatformRoleAccountResponse>(PlatformRoleErrors.AccountNotFound);
        }

        logger.LogInformation(
            "Admin {AdminId} looked up account {AccountId} in session {SessionId}.",
            query.AdminId,
            user.Id,
            query.AdminSessionId);

        var roles = await context.ListPlatformRolesAsync(user.Id, cancellationToken);

        return Result.Success(new PlatformRoleAccountResponse(
            user.Id,
            user.DisplayName,
            user.Email,
            user.EmailConfirmed,
            roles));
    }
}

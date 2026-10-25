using ReactionLab.Identity.Api.Features.AccountSetups.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.AccountSetups.LookUpAccountSetup;

internal sealed class LookUpAccountSetupHandler(AppDbContext context, TimeProvider timeProvider)
    : IQueryHandler<LookUpAccountSetupQuery, AccountSetupResponse>
{
    public async ValueTask<Result<AccountSetupResponse>> HandleAsync(
        LookUpAccountSetupQuery query,
        CancellationToken cancellationToken)
    {
        if (!AccountSetupTokens.TryHash(query.Token, out var tokenHash)
            || await context.FindPendingAccountSetupAsync(tokenHash, timeProvider.GetUtcNow(), cancellationToken)
                is not { } setup)
        {
            return Result.Failure<AccountSetupResponse>(AccountSetupErrors.LinkInvalid);
        }

        return Result.Success(new AccountSetupResponse(
            setup.DisplayName,
            setup.Username,
            setup.OrganizationName,
            setup.SignInCode,
            setup.ExpiresAt));
    }
}

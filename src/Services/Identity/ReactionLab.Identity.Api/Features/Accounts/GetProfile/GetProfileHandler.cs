using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.GetProfile;

internal sealed class GetProfileHandler(AppDbContext context) : IQueryHandler<GetProfileQuery, ProfileResponse>
{
    public async ValueTask<Result<ProfileResponse>> HandleAsync(
        GetProfileQuery query,
        CancellationToken cancellationToken)
    {
        if (await context.FindProfileAsync(query.AccountId, cancellationToken) is not { } profile)
        {
            return Result.Failure<ProfileResponse>(AccountErrors.NotFound);
        }

        return Result.Success(new ProfileResponse(profile.Email, profile.DisplayName, profile.PreferredLocale));
    }
}

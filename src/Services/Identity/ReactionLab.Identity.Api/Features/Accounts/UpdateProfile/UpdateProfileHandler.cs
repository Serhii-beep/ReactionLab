using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.UpdateProfile;

internal sealed class UpdateProfileHandler(AppDbContext context, ILogger<UpdateProfileHandler> logger)
    : ICommandHandler<UpdateProfileCommand, ProfileResponse>
{
    public async ValueTask<Result<ProfileResponse>> HandleAsync(
        UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        var displayName = command.DisplayName is null ? null : User.NormalizeDisplayName(command.DisplayName);

        if (displayName is { IsFailure: true })
        {
            return Result.Failure<ProfileResponse>(displayName.Error);
        }

        var locale = command.Locale is null ? null : SupportedLocale.Create(command.Locale);

        if (locale is { IsFailure: true })
        {
            return Result.Failure<ProfileResponse>(locale.Error);
        }

        var profile = await context.UpdateProfileAsync(command.AccountId, displayName?.Value, locale?.Value.Code);

        if (profile is null)
        {
            return Result.Failure<ProfileResponse>(AccountErrors.NotFound);
        }

        logger.LogInformation("Account {AccountId} updated its profile.", command.AccountId);

        return Result.Success(new ProfileResponse(profile.Email, profile.DisplayName, profile.PreferredLocale));
    }
}

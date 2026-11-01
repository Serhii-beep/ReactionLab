using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Accounts.Contracts;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Messaging.Outbox;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.UpdateProfile;

internal sealed class UpdateProfileHandler(
    AppDbContext context,
    OutboxWriter outbox,
    ILogger<UpdateProfileHandler> logger)
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

        if (displayName is not null && await context.IsSchoolAccountAsync(command.AccountId, cancellationToken))
        {
            return Result.Failure<ProfileResponse>(AccountErrors.ManagedBySchool);
        }

        var profile = await UpdateAsync(command.AccountId, displayName?.Value, locale?.Value.Code);

        if (profile is null)
        {
            return Result.Failure<ProfileResponse>(AccountErrors.NotFound);
        }

        logger.LogInformation("Account {AccountId} updated its profile.", command.AccountId);

        return Result.Success(new ProfileResponse(
            profile.Email,
            profile.DisplayName,
            profile.PreferredLocale,
            profile.HasPassword));
    }

    private Task<UpdatedProfile?> UpdateAsync(Guid accountId, string? displayName, string? locale) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken.None);

            var profile = await context.UpdateProfileAsync(accountId, displayName, locale);

            if (profile is { ProfileChanged: true })
            {
                await outbox.InsertAsync(
                    context,
                    new UserProfile(accountId, profile.Version, profile.DisplayName, profile.PreferredLocale));
            }

            await transaction.CommitAsync(CancellationToken.None);

            return profile;
        });
}

using System.Text.Json;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Erasures;
using ReactionLab.Messaging.Consuming;

namespace ReactionLab.Identity.Api.Features.Erasures;

internal sealed class ErasureConfirmations(
    AppDbContext context,
    ErasureMetrics metrics,
    TimeProvider timeProvider,
    ILogger<ErasureConfirmations> logger) : IConsumedEventEffects
{
    public static readonly IReadOnlyCollection<string> Topics =
        [.. AccountErasure.ConfirmingServices.Select(ErasureTopics.ConfirmationsOf)];

    public static readonly IReadOnlyCollection<string> EventTypes = [ErasureEventTypes.UserDataErased];

    public async Task ApplyAsync(ConsumedEvent consumed, CancellationToken cancellationToken)
    {
        var erased = consumed.Read(IdentityEventJsonContext.Default.UserDataErased);
        var service = ErasureTopics.ServiceOf(consumed.Topic);

        if (erased.UserId.ToString() != consumed.Subject || service is null || erased.Service != service)
        {
            throw new JsonException(
                $"{consumed.Type} on {consumed.Topic} names another account than its key, or another service than "
                + "its topic.");
        }

        if (await context.LockAccountErasureAsync(erased.UserId, cancellationToken) is not { } erasure)
        {
            logger.LogWarning(
                "{Service} confirmed erasing account {AccountId}, whose erasure Identity has no record of.",
                service,
                erased.UserId);

            return;
        }

        var confirmedAt = timeProvider.GetUtcNow();

        if (erased.Version < erasure.Version
            || await context.ConfirmAccountErasureAsync(erased.UserId, service, confirmedAt, cancellationToken) == 0)
        {
            logger.LogInformation(
                "{Service} confirmed erasing account {AccountId} at version {Version}, which the erasure doesn't "
                + "wait for.",
                service,
                erased.UserId,
                erased.Version);

            return;
        }

        if (await context.CompleteAccountErasureAsync(erased.UserId, confirmedAt, cancellationToken) == 1)
        {
            metrics.RecordCompleted(confirmedAt - erasure.RequestedAt);
            logger.LogInformation(
                "Every service confirmed erasing account {AccountId}, so its erasure is complete.",
                erased.UserId);
        }
    }
}

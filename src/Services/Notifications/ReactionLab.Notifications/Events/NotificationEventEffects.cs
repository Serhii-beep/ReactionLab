using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;

namespace ReactionLab.Notifications.Events;

internal sealed class NotificationEventEffects(
    RepeatedRegistrationNotices repeatedRegistrationNotices,
    EmailConfirmationRequests emailConfirmationRequests,
    AccountErasure accountErasure) : IConsumedEventEffects
{
    public static readonly IReadOnlyCollection<string> EventTypes =
    [
        IdentityEventTypes.RegistrationRepeated,
        IdentityEventTypes.EmailConfirmationRequested,
        IdentityEventTypes.UserErasureRequested
    ];

    public static readonly IReadOnlyCollection<string> ErasureEventTypes =
    [
        IdentityEventTypes.UserErasureRequested
    ];

    public Task ApplyAsync(ConsumedEvent consumed, CancellationToken cancellationToken) =>
        consumed.Type switch
        {
            IdentityEventTypes.RegistrationRepeated =>
                repeatedRegistrationNotices.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.EmailConfirmationRequested =>
                emailConfirmationRequests.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.UserErasureRequested =>
                accountErasure.EraseAsync(consumed, cancellationToken),
            _ => throw new InvalidOperationException($"Notifications has no effect for {consumed.Type}.")
        };
}

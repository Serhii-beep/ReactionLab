using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Notifications.Events;

internal static class NotificationEvents
{
    public const string Source = "urn:reactionlab:notifications";

    public static void Publish(PublishedEventTypes eventTypes) =>
        eventTypes.Add(
            ErasureTopics.ConfirmationsOf(ErasureServices.Notifications),
            ErasureEventTypes.UserDataErased,
            erased => erased.UserId,
            IdentityEventJsonContext.Default.UserDataErased);
}

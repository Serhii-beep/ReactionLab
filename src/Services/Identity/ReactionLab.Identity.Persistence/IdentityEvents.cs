using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Outbox;

namespace ReactionLab.Identity.Persistence;

public static class IdentityEvents
{
    public static void Publish(PublishedEventTypes eventTypes) =>
        eventTypes
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.UserRegistered,
                registered => registered.UserId,
                IdentityEventJsonContext.Default.UserRegistered)
            .AddState(
                IdentityTopics.Profiles,
                IdentityEventTypes.UserProfile,
                profile => profile.UserId,
                IdentityEventJsonContext.Default.UserProfile)
            .Add(
                IdentityTopics.Erasure,
                IdentityEventTypes.UserErasureRequested,
                erasure => erasure.UserId,
                IdentityEventJsonContext.Default.UserErasureRequested);
}

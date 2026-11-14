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
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.RegistrationRepeated,
                repeated => repeated.UserId,
                IdentityEventJsonContext.Default.RegistrationRepeated)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.EmailConfirmationRequested,
                requested => requested.UserId,
                IdentityEventJsonContext.Default.EmailConfirmationRequested)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.PasswordResetRequested,
                requested => requested.UserId,
                IdentityEventJsonContext.Default.PasswordResetRequested)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.PasswordChanged,
                changed => changed.UserId,
                IdentityEventJsonContext.Default.PasswordChanged)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.SignInMethodChanged,
                changed => changed.UserId,
                IdentityEventJsonContext.Default.SignInMethodChanged)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.AccountLocked,
                locked => locked.UserId,
                IdentityEventJsonContext.Default.AccountLocked)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.PlatformAdminChangeAnnounced,
                announced => announced.UserId,
                IdentityEventJsonContext.Default.PlatformAdminChangeAnnounced)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.SchoolAdminChangeAnnounced,
                announced => announced.UserId,
                IdentityEventJsonContext.Default.SchoolAdminChangeAnnounced)
            .Add(
                IdentityTopics.Users,
                IdentityEventTypes.AccountSetupRequested,
                requested => requested.UserId,
                IdentityEventJsonContext.Default.AccountSetupRequested)
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

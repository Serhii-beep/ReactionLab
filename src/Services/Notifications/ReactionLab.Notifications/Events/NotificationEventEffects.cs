using ReactionLab.Identity.Contracts;
using ReactionLab.Messaging.Consuming;

namespace ReactionLab.Notifications.Events;

internal sealed class NotificationEventEffects(
    RepeatedRegistrationNotices repeatedRegistrationNotices,
    EmailConfirmationRequests emailConfirmationRequests,
    PasswordResetRequests passwordResetRequests,
    PasswordChangeNotices passwordChangeNotices,
    SignInMethodChangeNotices signInMethodChangeNotices,
    LockoutNotices lockoutNotices,
    PlatformAdminChangeNotices platformAdminChangeNotices,
    SchoolAdminChangeNotices schoolAdminChangeNotices,
    AccountSetupRequests accountSetupRequests,
    AccountErasure accountErasure) : IConsumedEventEffects
{
    public static readonly IReadOnlyCollection<string> EventTypes =
    [
        IdentityEventTypes.RegistrationRepeated,
        IdentityEventTypes.EmailConfirmationRequested,
        IdentityEventTypes.PasswordResetRequested,
        IdentityEventTypes.PasswordChanged,
        IdentityEventTypes.SignInMethodChanged,
        IdentityEventTypes.AccountLocked,
        IdentityEventTypes.PlatformAdminChangeAnnounced,
        IdentityEventTypes.SchoolAdminChangeAnnounced,
        IdentityEventTypes.AccountSetupRequested,
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
            IdentityEventTypes.PasswordResetRequested =>
                passwordResetRequests.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.PasswordChanged =>
                passwordChangeNotices.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.SignInMethodChanged =>
                signInMethodChangeNotices.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.AccountLocked =>
                lockoutNotices.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.PlatformAdminChangeAnnounced =>
                platformAdminChangeNotices.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.SchoolAdminChangeAnnounced =>
                schoolAdminChangeNotices.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.AccountSetupRequested =>
                accountSetupRequests.QueueAsync(consumed, cancellationToken),
            IdentityEventTypes.UserErasureRequested =>
                accountErasure.EraseAsync(consumed, cancellationToken),
            _ => throw new InvalidOperationException($"Notifications has no effect for {consumed.Type}.")
        };
}

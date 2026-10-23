using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Passwords;

internal static class PasswordPolicy
{
    public const int MinimumLength = 15;
    public const int MaximumLength = 256;
    public const int HashIterations = 220_000;
    public const int CurrentPasswordChecksPerSession = 5;

    public static readonly LockoutSchedule Lockout = new(
        FailuresPerLockout: 10,
        FailuresBeforeDisabling: 100,
        FirstLockout: TimeSpan.FromMinutes(5));

    public static readonly TimeSpan MinimumFailedSignInDuration = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan Probation = TimeSpan.FromDays(7);
}

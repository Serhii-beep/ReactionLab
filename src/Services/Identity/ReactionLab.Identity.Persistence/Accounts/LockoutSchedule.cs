namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record LockoutSchedule(int FailuresPerLockout, int FailuresBeforeDisabling, TimeSpan FirstLockout);

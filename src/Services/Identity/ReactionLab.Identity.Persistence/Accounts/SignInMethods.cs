namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record SignInMethods(
    bool IsSchoolAccount,
    bool HasPassword,
    bool PasswordSignsIn,
    DateTimeOffset? PasswordProbationEndsAt,
    IReadOnlyList<ConnectedLogin> Logins)
{
    public bool IsPasswordOnProbation(DateTimeOffset now) => HasPassword && PasswordProbationEndsAt > now;

    public bool HasUsablePassword(DateTimeOffset now) => PasswordSignsIn && !IsPasswordOnProbation(now);

    public int CountUsableLogins(IReadOnlyCollection<string> usableProviders) =>
        Logins.Count(login => usableProviders.Contains(login.Provider));
}

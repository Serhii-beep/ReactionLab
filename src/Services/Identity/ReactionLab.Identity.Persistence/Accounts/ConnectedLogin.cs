namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record ConnectedLogin(string Provider, DateTimeOffset? ConnectedAt);

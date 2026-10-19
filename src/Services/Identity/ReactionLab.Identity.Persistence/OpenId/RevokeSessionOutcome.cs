namespace ReactionLab.Identity.Persistence.OpenId;

public sealed record RevokeSessionOutcome(bool ActingSessionValid, bool SessionFound, bool SessionEnded);

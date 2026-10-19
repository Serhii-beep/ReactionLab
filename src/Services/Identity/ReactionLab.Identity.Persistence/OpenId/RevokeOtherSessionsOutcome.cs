namespace ReactionLab.Identity.Persistence.OpenId;

public sealed record RevokeOtherSessionsOutcome(bool ActingSessionValid, int EndedSessionCount);

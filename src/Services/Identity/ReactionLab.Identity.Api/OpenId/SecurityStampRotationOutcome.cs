namespace ReactionLab.Identity.Api.OpenId;

internal sealed record SecurityStampRotationOutcome(
    bool AccountMatched,
    bool ActingSessionValid,
    int EndedSessionCount)
{
    public static readonly SecurityStampRotationOutcome AccountUnmatched = new(false, false, 0);

    public static readonly SecurityStampRotationOutcome ActingSessionEnded = new(true, false, 0);

    public static SecurityStampRotationOutcome Rotated(int endedSessionCount) => new(true, true, endedSessionCount);
}

namespace ReactionLab.Identity.Contracts;

public static class IdentityEventTypes
{
    public const string Source = "urn:reactionlab:identity";
    public const string UserRegistered = "reactionlab.identity.user-registered.v1";
    public const string RegistrationRepeated = "reactionlab.identity.registration-repeated.v1";
    public const string UserProfile = "reactionlab.identity.user-profile.v1";
    public const string UserErasureRequested = "reactionlab.identity.user-erasure-requested.v1";
}

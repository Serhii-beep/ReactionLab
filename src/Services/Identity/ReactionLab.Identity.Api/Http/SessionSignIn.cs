namespace ReactionLab.Identity.Api.Http;

internal sealed record SessionSignIn(DateTimeOffset? SignedInAt, bool WithPassword);

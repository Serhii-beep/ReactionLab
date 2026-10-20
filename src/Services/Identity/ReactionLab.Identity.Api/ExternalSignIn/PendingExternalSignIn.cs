namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed record PendingExternalSignIn(ExternalIdentity Identity, string ReturnUrl, DateTimeOffset SignedInAt);

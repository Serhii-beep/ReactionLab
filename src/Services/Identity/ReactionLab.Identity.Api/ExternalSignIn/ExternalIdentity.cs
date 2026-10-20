namespace ReactionLab.Identity.Api.ExternalSignIn;

internal sealed record ExternalIdentity(string Provider, string Subject, string? VerifiedEmail, string? Name);

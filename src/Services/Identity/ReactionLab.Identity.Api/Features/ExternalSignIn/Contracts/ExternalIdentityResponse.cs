namespace ReactionLab.Identity.Api.Features.ExternalSignIn.Contracts;

internal sealed record ExternalIdentityResponse(
    string Provider,
    string ProviderDisplayName,
    string? Email,
    string? SuggestedDisplayName,
    string NextStep,
    string ReturnUrl);

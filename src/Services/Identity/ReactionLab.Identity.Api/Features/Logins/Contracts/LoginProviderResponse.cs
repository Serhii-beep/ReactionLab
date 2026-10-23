namespace ReactionLab.Identity.Api.Features.Logins.Contracts;

internal sealed record LoginProviderResponse(
    string Name,
    string DisplayName,
    bool Connected,
    DateTimeOffset? ConnectedAt);

namespace ReactionLab.Identity.Api.Features.Logins.Contracts;

internal sealed record LoginsResponse(
    bool HasPassword,
    DateTimeOffset? PasswordProbationEndsAt,
    IReadOnlyList<LoginProviderResponse> Providers);

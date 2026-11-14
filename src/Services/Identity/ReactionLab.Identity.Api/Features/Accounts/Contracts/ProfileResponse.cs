namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record ProfileResponse(
    string? Email,
    string DisplayName,
    string Locale,
    string? TimeZone,
    bool HasPassword);

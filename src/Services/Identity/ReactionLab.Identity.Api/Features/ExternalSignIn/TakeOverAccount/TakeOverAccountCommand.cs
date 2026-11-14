namespace ReactionLab.Identity.Api.Features.ExternalSignIn.TakeOverAccount;

internal sealed record TakeOverAccountCommand(
    string? DisplayName,
    int? BirthYear,
    string Locale,
    string? TimeZone,
    string? UserAgent);

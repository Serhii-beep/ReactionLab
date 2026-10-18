namespace ReactionLab.Identity.Api.Features.Accounts.RegisterAccount;

internal sealed record RegisterAccountCommand(
    string? Email,
    string? Password,
    string? DisplayName,
    int? BirthYear,
    string Locale);

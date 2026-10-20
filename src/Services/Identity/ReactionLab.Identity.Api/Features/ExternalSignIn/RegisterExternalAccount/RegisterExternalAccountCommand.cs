namespace ReactionLab.Identity.Api.Features.ExternalSignIn.RegisterExternalAccount;

internal sealed record RegisterExternalAccountCommand(string? DisplayName, int? BirthYear, string Locale);

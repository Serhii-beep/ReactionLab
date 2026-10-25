namespace ReactionLab.Identity.Api.Features.AccountSetups.Contracts;

internal sealed record AccountSetupResponse(
    string DisplayName,
    string Username,
    string OrganizationName,
    string SignInCode,
    DateTimeOffset ExpiresAt);

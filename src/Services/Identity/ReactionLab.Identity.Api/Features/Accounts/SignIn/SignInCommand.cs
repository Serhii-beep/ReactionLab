namespace ReactionLab.Identity.Api.Features.Accounts.SignIn;

internal sealed record SignInCommand(string? Email, string? Password);

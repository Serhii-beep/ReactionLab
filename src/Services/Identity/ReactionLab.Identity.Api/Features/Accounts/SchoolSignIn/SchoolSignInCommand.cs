namespace ReactionLab.Identity.Api.Features.Accounts.SchoolSignIn;

internal sealed record SchoolSignInCommand(string? SignInCode, string? Username, string? Password);

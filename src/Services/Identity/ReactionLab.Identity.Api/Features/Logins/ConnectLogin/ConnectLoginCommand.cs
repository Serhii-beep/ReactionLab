namespace ReactionLab.Identity.Api.Features.Logins.ConnectLogin;

internal sealed record ConnectLoginCommand(Guid AccountId, Guid CurrentSessionId, string Provider, string? ReturnUrl);

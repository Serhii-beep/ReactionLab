namespace ReactionLab.Identity.Api.Features.Logins;

internal sealed record LoginConnection(Guid AccountId, Guid SessionId);

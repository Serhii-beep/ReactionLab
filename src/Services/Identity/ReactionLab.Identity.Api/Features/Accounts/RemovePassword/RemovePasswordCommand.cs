using ReactionLab.Identity.Api.Http;

namespace ReactionLab.Identity.Api.Features.Accounts.RemovePassword;

internal sealed record RemovePasswordCommand(Guid AccountId, Guid CurrentSessionId, SessionSignIn SignIn);

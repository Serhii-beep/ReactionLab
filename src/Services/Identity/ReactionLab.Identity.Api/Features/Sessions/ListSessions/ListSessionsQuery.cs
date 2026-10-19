using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Sessions.ListSessions;

internal sealed record ListSessionsQuery(Guid AccountId, Guid CurrentSessionId, CursorRequest Page);

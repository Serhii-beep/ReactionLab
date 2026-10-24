using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleAccounts;

internal sealed record ListPlatformRoleAccountsQuery(CursorRequest Page);

using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.ListPlatformRoleChanges;

internal sealed record ListPlatformRoleChangesQuery(Guid? AccountId, CursorRequest Page);

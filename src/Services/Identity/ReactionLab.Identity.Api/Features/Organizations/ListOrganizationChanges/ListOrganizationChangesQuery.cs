using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Organizations.ListOrganizationChanges;

internal sealed record ListOrganizationChangesQuery(Guid ActorId, Guid OrganizationId, CursorRequest Page);

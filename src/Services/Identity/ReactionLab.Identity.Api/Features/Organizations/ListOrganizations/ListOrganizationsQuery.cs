using ReactionLab.SharedKernel.Pagination;

namespace ReactionLab.Identity.Api.Features.Organizations.ListOrganizations;

internal sealed record ListOrganizationsQuery(Guid? DistrictId, CursorRequest Page);

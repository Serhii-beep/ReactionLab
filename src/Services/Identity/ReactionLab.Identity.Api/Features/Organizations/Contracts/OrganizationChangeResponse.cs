using ReactionLab.Identity.Persistence.Organizations;

namespace ReactionLab.Identity.Api.Features.Organizations.Contracts;

internal sealed record OrganizationChangeResponse(
    Guid Id,
    string Change,
    Guid? AccountId,
    string? Role,
    Guid? ChangedBy,
    DateTimeOffset ChangedAt)
{
    public static OrganizationChangeResponse From(OrganizationChange change) =>
        new(change.Id, change.Change, change.AccountId, change.Role, change.ChangedBy, change.ChangedAt);
}

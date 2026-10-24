namespace ReactionLab.Identity.Api.Features.PlatformRoles.Contracts;

internal sealed record PlatformRoleChangeResponse(
    Guid Id,
    Guid AccountId,
    string? AccountDisplayName,
    string Role,
    string Change,
    string Source,
    Guid? ChangedBy,
    string? ChangedByDisplayName,
    DateTimeOffset ChangedAt);

namespace ReactionLab.Identity.Persistence.Erasures;

public sealed record EscalatedErasure(
    Guid UserId,
    long Version,
    Guid? OrganizationId,
    DateTimeOffset RequestedAt,
    string? PendingServices);

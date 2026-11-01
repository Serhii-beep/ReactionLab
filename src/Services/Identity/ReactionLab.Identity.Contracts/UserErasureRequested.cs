using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record UserErasureRequested(Guid UserId, long Version, Guid? OrganizationId) : DomainEvent;

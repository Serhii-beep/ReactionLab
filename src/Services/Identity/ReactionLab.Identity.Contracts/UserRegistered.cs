using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record UserRegistered(Guid UserId, long Version, string AccountKind, Guid? OrganizationId) : DomainEvent;

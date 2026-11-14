using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record AccountLocked(Guid UserId, long Version, string Lock, bool Disabled) : DomainEvent;

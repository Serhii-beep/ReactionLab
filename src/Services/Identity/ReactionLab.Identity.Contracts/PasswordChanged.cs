using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record PasswordChanged(Guid UserId, long Version, Guid ChangeId) : DomainEvent;

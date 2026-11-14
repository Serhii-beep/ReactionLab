using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record PasswordResetRequested(Guid UserId, long Version, Guid RequestId) : DomainEvent;

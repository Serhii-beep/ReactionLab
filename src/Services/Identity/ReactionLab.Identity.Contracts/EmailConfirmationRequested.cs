using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record EmailConfirmationRequested(Guid UserId, long Version, Guid RequestId) : DomainEvent;

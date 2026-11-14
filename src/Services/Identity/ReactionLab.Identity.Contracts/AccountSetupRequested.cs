using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record AccountSetupRequested(Guid UserId, long Version, Guid RequestId, string Purpose) : DomainEvent;

using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record RegistrationRepeated(Guid UserId, long Version) : DomainEvent;

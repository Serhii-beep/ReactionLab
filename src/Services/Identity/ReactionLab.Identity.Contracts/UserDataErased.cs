using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record UserDataErased(Guid UserId, long Version, string Service) : DomainEvent;

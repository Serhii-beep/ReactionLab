using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record PlatformAdminChangeAnnounced(Guid UserId, long Version, Guid ChangeId) : DomainEvent;

using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record SchoolAdminChangeAnnounced(Guid UserId, long Version, Guid ChangeId) : DomainEvent;

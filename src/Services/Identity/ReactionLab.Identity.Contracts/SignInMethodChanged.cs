using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record SignInMethodChanged(Guid UserId, long Version, Guid ChangeId) : DomainEvent;

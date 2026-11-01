using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Contracts;

public sealed record UserProfile(Guid UserId, long Version, string DisplayName, string Locale) : DomainEvent;

namespace ReactionLab.Identity.Persistence.Accounts;

public sealed record StoredEmail(Guid AccountId, Guid? OrganizationId, string Email);

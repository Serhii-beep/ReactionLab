namespace ReactionLab.Identity.Persistence.AccountSetups;

public sealed record AdminSetupState(string Username, bool HasPassword, bool OrganizationHasAdminWithPassword);

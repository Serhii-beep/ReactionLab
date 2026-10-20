namespace ReactionLab.Identity.Api.Features.Accounts.UpdateProfile;

internal sealed record UpdateProfileCommand(Guid AccountId, string? DisplayName, string? Locale);

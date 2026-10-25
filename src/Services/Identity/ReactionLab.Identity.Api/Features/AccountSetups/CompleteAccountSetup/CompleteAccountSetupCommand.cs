namespace ReactionLab.Identity.Api.Features.AccountSetups.CompleteAccountSetup;

internal sealed record CompleteAccountSetupCommand(string? Token, string? NewPassword);

using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record ReauthenticateRequest(
    [property: Required]
    string? CurrentPassword);

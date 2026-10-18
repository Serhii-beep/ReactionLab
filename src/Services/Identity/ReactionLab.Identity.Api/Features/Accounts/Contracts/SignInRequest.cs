using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record SignInRequest(
    [property: Required]
    string? Email,
    [property: Required]
    string? Password);

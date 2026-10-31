using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record SchoolSignInRequest(
    [property: Required]
    string? SignInCode,
    [property: Required]
    string? Username,
    [property: Required]
    string? Password);

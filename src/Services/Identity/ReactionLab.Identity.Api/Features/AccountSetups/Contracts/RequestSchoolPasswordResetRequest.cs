using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Identity.Api.Features.AccountSetups.Contracts;

internal sealed record RequestSchoolPasswordResetRequest(
    [property: Required]
    string? SignInCode,
    [property: Required]
    string? Username);

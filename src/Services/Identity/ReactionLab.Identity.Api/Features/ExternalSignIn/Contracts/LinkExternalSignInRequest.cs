using System.ComponentModel.DataAnnotations;

namespace ReactionLab.Identity.Api.Features.ExternalSignIn.Contracts;

internal sealed record LinkExternalSignInRequest(
    [property: Required]
    string? Password);

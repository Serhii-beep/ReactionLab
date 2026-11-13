using System.ComponentModel.DataAnnotations;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Accounts.Contracts;

internal sealed record RequestEmailConfirmationRequest(
    [property: Required]
    [property: MaxLength(User.MaximumEmailLength)]
    string? Email);

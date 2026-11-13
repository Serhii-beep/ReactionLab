using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Persistence.Accounts;

namespace ReactionLab.Identity.Api.Features.Accounts;

internal sealed class AccountConfirmation : IUserConfirmation<User>
{
    public Task<bool> IsConfirmedAsync(UserManager<User> manager, User user) =>
        Task.FromResult(user.EmailConfirmed
            || user.EmailConfirmationExempt
            || user.Email is null
            || user.OrganizationId is not null);
}

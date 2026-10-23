using Microsoft.EntityFrameworkCore;

namespace ReactionLab.Identity.Persistence.Accounts;

public static class LoginQueries
{
    public static Task<SignInMethods?> FindSignInMethodsAsync(
        this AppDbContext context,
        Guid userId,
        CancellationToken cancellationToken) =>
        context.Users.AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new SignInMethods(
                user.PasswordHash != null,
                user.PasswordHash != null && user.NormalizedEmail != null && user.LockoutEnd != DateTimeOffset.MaxValue,
                user.PasswordProbationEndsAt,
                context.UserLogins
                    .Where(login => login.UserId == user.Id)
                    .OrderBy(login => login.LoginProvider)
                    .Select(login => new ConnectedLogin(
                        login.LoginProvider,
                        EF.Property<DateTimeOffset?>(login, UserLoginProperties.ConnectedAt)))
                    .ToList()))
            .SingleOrDefaultAsync(cancellationToken);
}

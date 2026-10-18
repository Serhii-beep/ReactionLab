using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.RegisterAccount;

internal sealed class RegisterAccountHandler(
    UserManager<User> userManager,
    TimeProvider timeProvider,
    ILogger<RegisterAccountHandler> logger) : ICommandHandler<RegisterAccountCommand>
{
    public async ValueTask<Result> HandleAsync(RegisterAccountCommand command, CancellationToken cancellationToken)
    {
        var locale = SupportedLocale.Create(command.Locale);

        if (locale.IsFailure)
        {
            return Result.Failure(locale.Error);
        }

        var user = User.Register(
            command.Email,
            command.DisplayName,
            locale.Value,
            command.BirthYear,
            timeProvider.GetUtcNow());

        if (user.IsFailure)
        {
            return Result.Failure(user.Error);
        }

        var created = await CreateAsync(user.Value, command.Password ?? string.Empty);

        if (created.Succeeded)
        {
            logger.LogInformation("Registered account {UserId}.", user.Value.Id);

            return Result.Success();
        }

        if (created.Errors.Any(error => error.IsAlreadyRegistered()))
        {
            logger.LogInformation("Registration for an address that already has an account.");

            return Result.Success();
        }

        return Result.Failure(created.Errors.First().ToError());
    }

    private async Task<IdentityResult> CreateAsync(User user, string password)
    {
        try
        {
            return await userManager.CreateAsync(user, password);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return IdentityResult.Failed(userManager.ErrorDescriber.DuplicateEmail(user.Email ?? string.Empty));
        }
    }
}

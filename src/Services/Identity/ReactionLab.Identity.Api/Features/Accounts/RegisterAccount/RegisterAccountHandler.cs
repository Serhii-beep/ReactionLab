using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using ReactionLab.Identity.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.SecurityLinks;
using ReactionLab.Messaging.Outbox;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Accounts.RegisterAccount;

internal sealed class RegisterAccountHandler(
    UserManager<User> userManager,
    AppDbContext context,
    OutboxWriter outbox,
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

        var now = timeProvider.GetUtcNow();
        var user = User.Register(
            command.Email,
            command.DisplayName,
            locale.Value,
            command.BirthYear,
            command.TimeZone,
            now);

        if (user.IsFailure)
        {
            return Result.Failure(user.Error);
        }

        var link = SecurityLink.RequestEmailConfirmation(user.Value, now);

        user.Value.RecordEmailConfirmationRequest(link.RequestId);
        context.Add(link);

        var created = await CreateAsync(user.Value, command.Password ?? string.Empty);

        if (created.Succeeded)
        {
            logger.LogInformation("Registered account {UserId}.", user.Value.Id);

            if (command.TimeZone is { Length: > 0 } && user.Value.TimeZone is null)
            {
                logger.LogInformation(
                    "Account {UserId} named a time zone this host doesn't know, so it has none.",
                    user.Value.Id);
            }

            return Result.Success();
        }

        if (created.Errors.Any(error => error.IsAlreadyRegistered()))
        {
            await AnswerRepeatedRegistrationAsync(user.Value.Email ?? string.Empty);
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

    private async Task AnswerRepeatedRegistrationAsync(string email)
    {
        try
        {
            await context.Database.CreateExecutionStrategy().ExecuteAsync(
                Guid.CreateVersion7(),
                async (_, eventId, cancellationToken) =>
                {
                    context.ChangeTracker.Clear();

                    if (await userManager.FindByEmailAsync(email) is not { } account)
                    {
                        return false;
                    }

                    if (account is { EmailConfirmed: false, EmailConfirmationExempt: false, PasswordHash: not null })
                    {
                        var requested = new EmailConfirmationRequested(account.Id, account.Version, eventId)
                        {
                            EventId = eventId
                        };

                        return await context.RequestEmailConfirmationAsync(
                            account.Id,
                            eventId,
                            outbox.CreateMessage(requested)) > 0;
                    }

                    account.RecordRepeatedRegistration(eventId);

                    return await context.SaveChangesAsync(cancellationToken) > 0;
                },
                async (_, eventId, cancellationToken) => new ExecutionResult<bool>(
                    await context.Set<OutboxMessage>().AnyAsync(message => message.Id == eventId, cancellationToken),
                    true),
                CancellationToken.None);
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }
}

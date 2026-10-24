using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.PlatformRoles;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.PlatformRoles.GrantPlatformRole;

internal sealed class GrantPlatformRoleHandler(
    RecentSignInCheck recentSignInCheck,
    PlatformRoleChangeTransaction changeTransaction,
    AppDbContext context,
    IHostEnvironment environment,
    ILogger<GrantPlatformRoleHandler> logger) : ICommandHandler<GrantPlatformRoleCommand>
{
    public async ValueTask<Result> HandleAsync(GrantPlatformRoleCommand command, CancellationToken cancellationToken)
    {
        if (!PlatformRoleNames.IsKnown(command.Role))
        {
            return Result.Failure(PlatformRoleErrors.Unknown);
        }

        if (await FindRefusalAsync(command.AccountId, cancellationToken) is { } refusal)
        {
            return Result.Failure(refusal);
        }

        var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
            command.AdminId,
            command.AdminSessionId,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not grant {Role} to account {AccountId}: {ErrorCode}.",
                command.AdminSessionId,
                command.Role,
                command.AccountId,
                proof.Error.Code);

            return proof;
        }

        var granted = await changeTransaction.RunAsync(
            command.AdminId,
            command.AdminSessionId,
            () => GrantAsync(command));

        if (granted.IsFailure)
        {
            return Result.Failure(granted.Error);
        }

        if (granted.Value)
        {
            logger.LogInformation(
                "Admin {AdminId} granted {Role} to account {AccountId} in session {SessionId}.",
                command.AdminId,
                command.Role,
                command.AccountId,
                command.AdminSessionId);
        }

        return Result.Success();
    }

    private async Task<Result<bool>> GrantAsync(GrantPlatformRoleCommand command)
    {
        if (await FindRefusalAsync(command.AccountId, CancellationToken.None) is { } refusal)
        {
            return Result.Failure<bool>(refusal);
        }

        var granted = await context.GrantPlatformRoleAsync(
            command.AccountId,
            command.Role,
            PlatformRoleChangeAuthor.Admin(command.AdminId, command.AdminSessionId));

        return Result.Success(granted == 1);
    }

    private async Task<Error?> FindRefusalAsync(Guid accountId, CancellationToken cancellationToken) =>
        await context.FindPlatformRoleCandidateAsync(accountId, cancellationToken) switch
        {
            null => PlatformRoleErrors.AccountNotFound,
            { EmailConfirmed: false } when !environment.IsDevelopment() => PlatformRoleErrors.AddressUnconfirmed,
            _ => null
        };
}

using Npgsql;
using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.RenameMember;

internal sealed class RenameMemberHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<RenameMemberHandler> logger) : ICommandHandler<RenameMemberCommand, MemberResponse>
{
    public async ValueTask<Result<MemberResponse>> HandleAsync(
        RenameMemberCommand command,
        CancellationToken cancellationToken)
    {
        var access = await adminTransaction.CheckAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            cancellationToken);

        if (access.IsFailure)
        {
            return Result.Failure<MemberResponse>(access.Error);
        }

        if (command.DisplayName is null && command.Username is null)
        {
            return Result.Failure<MemberResponse>(MemberErrors.NothingToChange);
        }

        var displayName = command.DisplayName is null ? null : User.NormalizeDisplayName(command.DisplayName);
        var username = command.Username is null ? null : User.NormalizeOrganizationUsername(command.Username);

        if (new[] { displayName, username }.FirstOrDefault(result => result is { IsFailure: true }) is { } invalid)
        {
            return Result.Failure<MemberResponse>(invalid.Error);
        }

        var now = timeProvider.GetUtcNow();
        var member = MemberTargets.Managed(
            await context.FindMemberAsync(
                command.OrganizationId,
                access.Value.DistrictId,
                command.AccountId,
                now,
                cancellationToken),
            command.OrganizationId);

        if (member.IsFailure)
        {
            return Result.Failure<MemberResponse>(member.Error);
        }

        if (username is not null && username.Value != member.Value.Username
            && (await context.ListTakenUsernamesAsync(command.OrganizationId, [username.Value], cancellationToken))
                .Count > 0)
        {
            return Result.Failure<MemberResponse>(MemberErrors.UsernameInUse);
        }

        var renamed = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            [],
            command.AccountId,
            lockedAccess => RenameAsync(command, lockedAccess, displayName?.Value, username?.Value, now));

        if (renamed.IsFailure)
        {
            return Result.Failure<MemberResponse>(renamed.Error);
        }

        logger.LogInformation(
            "Account {ActorId} renamed account {AccountId} in session {SessionId}.",
            command.ActorId,
            command.AccountId,
            command.ActorSessionId);

        return Result.Success(MemberResponse.From(renamed.Value, now));
    }

    private async Task<Result<SchoolMember>> RenameAsync(
        RenameMemberCommand command,
        OrganizationAccess access,
        string? displayName,
        string? username,
        DateTimeOffset now)
    {
        try
        {
            if (await context.RenameMemberAsync(command.AccountId, command.OrganizationId, displayName, username) != 1)
            {
                return MemberErrors.NotFound;
            }
        }
        catch (PostgresException ex) when (MemberConflicts.Of(ex) is { } conflict)
        {
            return conflict;
        }

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.AccountRenamed,
            command.AccountId,
            role: null,
            command.ActorId,
            command.ActorSessionId);

        return await context.FindMemberAsync(
            command.OrganizationId,
            access.DistrictId,
            command.AccountId,
            now,
            CancellationToken.None) ?? throw new InvalidOperationException("The renamed member cannot be read back.");
    }
}

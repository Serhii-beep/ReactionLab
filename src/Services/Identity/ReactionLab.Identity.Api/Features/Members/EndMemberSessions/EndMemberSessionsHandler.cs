using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.EndMemberSessions;

internal sealed class EndMemberSessionsHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<EndMemberSessionsHandler> logger) : ICommandHandler<EndMemberSessionsCommand>
{
    public async ValueTask<Result> HandleAsync(EndMemberSessionsCommand command, CancellationToken cancellationToken)
    {
        var access = await adminTransaction.CheckAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            cancellationToken);

        if (access.IsFailure)
        {
            return access;
        }

        var member = await FindAsync(command, access.Value, cancellationToken);

        if (member.IsFailure)
        {
            return member;
        }

        var ended = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            [],
            command.AccountId,
            lockedAccess => EndAsync(command, lockedAccess));

        if (ended.IsFailure)
        {
            return ended;
        }

        logger.LogInformation(
            "Account {ActorId} ended {SessionCount} session(s) of account {AccountId} in session {SessionId}.",
            command.ActorId,
            ended.Value,
            command.AccountId,
            command.ActorSessionId);

        return Result.Success();
    }

    private async Task<Result<int>> EndAsync(EndMemberSessionsCommand command, OrganizationAccess access)
    {
        var member = await FindAsync(command, access, CancellationToken.None);

        if (member.IsFailure)
        {
            return member.Error;
        }

        var endedSessions = await MemberSessions.EndAllAsync(context, command.AccountId);

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.SessionsEnded,
            command.AccountId,
            role: null,
            command.ActorId,
            command.ActorSessionId);

        return endedSessions;
    }

    private async Task<Result<SchoolMember>> FindAsync(
        EndMemberSessionsCommand command,
        OrganizationAccess access,
        CancellationToken cancellationToken) =>
        MemberTargets.Managed(
            await context.FindMemberAsync(
                command.OrganizationId,
                access.DistrictId,
                command.AccountId,
                timeProvider.GetUtcNow(),
                cancellationToken),
            command.OrganizationId);
}

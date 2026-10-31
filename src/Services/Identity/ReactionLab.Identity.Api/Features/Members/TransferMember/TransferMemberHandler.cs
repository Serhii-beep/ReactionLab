using Npgsql;
using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Api.Features.Organizations;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.TransferMember;

internal sealed class TransferMemberHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<TransferMemberHandler> logger) : ICommandHandler<TransferMemberCommand, MemberResponse>
{
    public async ValueTask<Result<MemberResponse>> HandleAsync(
        TransferMemberCommand command,
        CancellationToken cancellationToken)
    {
        var access = await adminTransaction.CheckAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.DistrictAdmin,
            cancellationToken);

        if (access.IsFailure)
        {
            return Result.Failure<MemberResponse>(access.Error);
        }

        if (command.DestinationId is not { } destinationId)
        {
            return Result.Failure<MemberResponse>(MemberErrors.DestinationRequired);
        }

        var student = await CheckAsync(command, access.Value, cancellationToken);

        if (student.IsFailure)
        {
            return Result.Failure<MemberResponse>(student.Error);
        }

        if (await CheckDestinationAsync(destinationId, access.Value, cancellationToken) is { } refusal)
        {
            return Result.Failure<MemberResponse>(refusal);
        }

        var taken = await context.ListTakenUsernamesAsync(destinationId, [student.Value.Username], cancellationToken);

        if (taken.Count > 0)
        {
            return Result.Failure<MemberResponse>(MemberErrors.UsernameInUse);
        }

        var now = timeProvider.GetUtcNow();
        var transferred = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.DistrictAdmin,
            [destinationId],
            command.AccountId,
            lockedAccess => TransferAsync(command, lockedAccess, destinationId, now));

        if (transferred.IsFailure)
        {
            return Result.Failure<MemberResponse>(transferred.Error);
        }

        if (transferred.Value.Moved)
        {
            logger.LogInformation(
                "Account {ActorId} moved {AccountId} from {OrganizationId} to {DestinationId} in session {SessionId}.",
                command.ActorId,
                command.AccountId,
                command.OrganizationId,
                destinationId,
                command.ActorSessionId);
        }

        return Result.Success(MemberResponse.From(transferred.Value.Member, now));
    }

    private async Task<Result<(SchoolMember Member, bool Moved)>> TransferAsync(
        TransferMemberCommand command,
        OrganizationAccess access,
        Guid destinationId,
        DateTimeOffset now)
    {
        if (await CheckDestinationAsync(destinationId, access, CancellationToken.None) is { } refusal)
        {
            return refusal;
        }

        var alreadyMoved = await context.FindMemberAsync(
            destinationId,
            access.DistrictId,
            command.AccountId,
            now,
            CancellationToken.None);

        if (alreadyMoved is { Roles: [SchoolRoleNames.Student] } && alreadyMoved.OrganizationId == destinationId)
        {
            return (alreadyMoved, false);
        }

        var student = await CheckAsync(command, access, CancellationToken.None);

        if (student.IsFailure)
        {
            return student.Error;
        }

        try
        {
            if (await context.TransferMemberAsync(command.AccountId, command.OrganizationId, destinationId) != 1)
            {
                return MemberErrors.NotTransferable;
            }
        }
        catch (PostgresException ex) when (MemberConflicts.Of(ex) is { } conflict)
        {
            return conflict;
        }

        await MemberSessions.EndAllAsync(context, command.AccountId);

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.TransferredOut,
            command.AccountId,
            SchoolRoleNames.Student,
            command.ActorId,
            command.ActorSessionId);

        await context.RecordOrganizationChangeAsync(
            destinationId,
            OrganizationChangeNames.TransferredIn,
            command.AccountId,
            SchoolRoleNames.Student,
            command.ActorId,
            command.ActorSessionId);

        var moved = await context.FindMemberAsync(
            destinationId,
            access.DistrictId,
            command.AccountId,
            now,
            CancellationToken.None);

        return (moved ?? throw new InvalidOperationException("The moved member cannot be read back."), true);
    }

    private async Task<Result<SchoolMember>> CheckAsync(
        TransferMemberCommand command,
        OrganizationAccess access,
        CancellationToken cancellationToken)
    {
        var member = MemberTargets.Managed(
            await context.FindMemberAsync(
                command.OrganizationId,
                access.DistrictId,
                command.AccountId,
                timeProvider.GetUtcNow(),
                cancellationToken),
            command.OrganizationId);

        if (member.IsFailure)
        {
            return member;
        }

        var movable = access.DistrictId is not null
            && member.Value.Roles is [SchoolRoleNames.Student]
            && command.DestinationId != command.OrganizationId;

        return movable ? member : MemberErrors.NotTransferable;
    }

    private async Task<Error?> CheckDestinationAsync(
        Guid destinationId,
        OrganizationAccess access,
        CancellationToken cancellationToken)
    {
        var destination = await context.FindOrganizationAsync(destinationId, cancellationToken);

        if (destination is not { Kind: OrganizationKinds.School } || destination.DistrictId != access.DistrictId)
        {
            return MemberErrors.DestinationNotFound;
        }

        return destination.SuspendedAt is null ? null : OrganizationErrors.Suspended;
    }
}

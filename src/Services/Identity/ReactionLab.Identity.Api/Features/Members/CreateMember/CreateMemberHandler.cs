using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Api.Features.Organizations;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.CreateMember;

internal sealed class CreateMemberHandler(
    SchoolAdminTransaction adminTransaction,
    MemberCredentials memberCredentials,
    UserManager<User> userManager,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<CreateMemberHandler> logger) : ICommandHandler<CreateMemberCommand, CreatedMemberResponse>
{
    public async ValueTask<Result<CreatedMemberResponse>> HandleAsync(
        CreateMemberCommand command,
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
            return Result.Failure<CreatedMemberResponse>(access.Error);
        }

        var roleOrganizationId = await PlaceRoleAsync(command, access.Value, cancellationToken);
        var kind = MemberCredentials.ReadKind(command.Credential);
        var locale = SupportedLocale.Create(command.Locale ?? access.Value.Locale);

        if (new Result[] { roleOrganizationId, kind, locale }.FirstOrDefault(result => result.IsFailure) is { } invalid)
        {
            return Result.Failure<CreatedMemberResponse>(invalid.Error);
        }

        var now = timeProvider.GetDatabaseUtcNow();
        var account = User.CreateSchoolAccount(
            command.OrganizationId,
            command.DisplayName,
            command.Username,
            command.Email,
            locale.Value,
            now);

        if (account.IsFailure)
        {
            return Result.Failure<CreatedMemberResponse>(account.Error);
        }

        var taken = await context.ListTakenUsernamesAsync(
            command.OrganizationId,
            [account.Value.OrganizationUsername!],
            cancellationToken);

        if (taken.Count > 0)
        {
            return Result.Failure<CreatedMemberResponse>(MemberErrors.UsernameInUse);
        }

        var credential = await memberCredentials.PrepareAsync(account.Value, kind.Value, cancellationToken);
        var created = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            roleOrganizationId.Value == command.OrganizationId ? [] : [roleOrganizationId.Value],
            changedAccountId: null,
            _ => CreateAsync(command, account.Value, roleOrganizationId.Value, credential, now));

        if (created.IsFailure)
        {
            return Result.Failure<CreatedMemberResponse>(created.Error);
        }

        logger.LogInformation(
            "Account {ActorId} created account {AccountId} at organization {OrganizationId} in session {SessionId}.",
            command.ActorId,
            account.Value.Id,
            command.OrganizationId,
            command.ActorSessionId);

        return Result.Success(new CreatedMemberResponse(
            MemberResponse.From(created.Value, now),
            credential.ToResponse()));
    }

    private async Task<Result<Guid>> PlaceRoleAsync(
        CreateMemberCommand command,
        OrganizationAccess access,
        CancellationToken cancellationToken)
    {
        if (command.Role is not { } role || !SchoolRoleNames.All.Contains(role))
        {
            return MemberErrors.RoleUnknown;
        }

        if (command.SchoolId is not { } schoolId)
        {
            return SchoolRoleNames.FitsKind(role, access.Kind) ? command.OrganizationId : MemberErrors.RoleUnknown;
        }

        if (role == SchoolRoleNames.Student)
        {
            return MemberErrors.StudentAtOwnSchoolOnly;
        }

        var school = await context.FindOrganizationAsync(schoolId, cancellationToken);

        if (access.Kind != OrganizationKinds.District || school?.DistrictId != command.OrganizationId)
        {
            return MemberErrors.SchoolNotFound;
        }

        if (school.SuspendedAt is not null)
        {
            return OrganizationErrors.Suspended;
        }

        return SchoolRoleNames.FitsKind(role, school.Kind) ? schoolId : MemberErrors.RoleUnknown;
    }

    private async Task<Result<SchoolMember>> CreateAsync(
        CreateMemberCommand command,
        User account,
        Guid roleOrganizationId,
        MemberCredential credential,
        DateTimeOffset now)
    {
        if (await context.FindMemberAsync(command.OrganizationId, null, account.Id, now, CancellationToken.None)
            is { } alreadyCreated)
        {
            return alreadyCreated;
        }

        if (roleOrganizationId != command.OrganizationId
            && await context.FindOrganizationAsync(roleOrganizationId, CancellationToken.None)
                is { SuspendedAt: not null })
        {
            return OrganizationErrors.Suspended;
        }

        context.ChangeTracker.Clear();

        try
        {
            var saved = await userManager.CreateAsync(account);

            if (!saved.Succeeded)
            {
                return saved.Errors.First().ToError();
            }
        }
        catch (DbUpdateException ex) when (MemberConflicts.Of(ex) is { } conflict)
        {
            return conflict;
        }

        var granted = await context.GrantMemberRoleAsync(account.Id, roleOrganizationId, command.Role!);

        if (granted != 1
            || !await memberCredentials.ApplyAsync(
                credential,
                account.Id,
                command.OrganizationId,
                command.ActorId,
                command.ActorSessionId))
        {
            throw new InvalidOperationException("The new member's role or credential was not saved.");
        }

        var ownRole = roleOrganizationId == command.OrganizationId ? command.Role : null;

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.AccountCreated,
            account.Id,
            ownRole,
            command.ActorId,
            command.ActorSessionId);

        if (ownRole is null)
        {
            await context.RecordOrganizationChangeAsync(
                roleOrganizationId,
                OrganizationChangeNames.RoleGranted,
                account.Id,
                command.Role,
                command.ActorId,
                command.ActorSessionId);
        }

        return await context.FindMemberAsync(command.OrganizationId, null, account.Id, now, CancellationToken.None)
            ?? throw new InvalidOperationException("The new member cannot be read back.");
    }
}

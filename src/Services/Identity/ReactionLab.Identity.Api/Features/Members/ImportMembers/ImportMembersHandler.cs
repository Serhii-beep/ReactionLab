using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.Members.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Members.ImportMembers;

internal sealed class ImportMembersHandler(
    SchoolAdminTransaction adminTransaction,
    MemberCredentials memberCredentials,
    UserManager<User> userManager,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<ImportMembersHandler> logger) : ICommandHandler<ImportMembersCommand, ImportedMembersResponse>
{
    public async ValueTask<Result<ImportedMembersResponse>> HandleAsync(
        ImportMembersCommand command,
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
            return Result.Failure<ImportedMembersResponse>(access.Error);
        }

        if (access.Value.Kind != OrganizationKinds.School)
        {
            return Result.Failure<ImportedMembersResponse>(MemberImportErrors.SchoolsOnly);
        }

        var kind = MemberCredentials.ReadKind(command.Credential);

        if (kind.IsFailure)
        {
            return Result.Failure<ImportedMembersResponse>(kind.Error);
        }

        var accounts = await ReadAccountsAsync(command, access.Value, cancellationToken);

        if (accounts.IsFailure)
        {
            return Result.Failure<ImportedMembersResponse>(accounts.Error);
        }

        var credentials = new List<MemberCredential>();

        foreach (var (_, account) in accounts.Value)
        {
            credentials.Add(await memberCredentials.PrepareAsync(account, kind.Value, cancellationToken));
        }

        var now = timeProvider.GetUtcNow();
        var imported = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            [],
            changedAccountId: null,
            _ => ImportAsync(command, accounts.Value, credentials, now));

        if (imported.IsFailure)
        {
            return Result.Failure<ImportedMembersResponse>(imported.Error);
        }

        logger.LogInformation(
            "Account {ActorId} imported {AccountCount} account(s) at {OrganizationId} in session {SessionId}.",
            command.ActorId,
            imported.Value.Count,
            command.OrganizationId,
            command.ActorSessionId);

        return Result.Success(new ImportedMembersResponse(imported.Value
            .Select((member, index) => new CreatedMemberResponse(
                MemberResponse.From(member, now),
                credentials[index].ToResponse()))
            .ToList()));
    }

    private async Task<Result<IReadOnlyList<(MemberCsvRow Row, User Account)>>> ReadAccountsAsync(
        ImportMembersCommand command,
        OrganizationAccess access,
        CancellationToken cancellationToken)
    {
        var rows = MemberCsv.Parse(command.Csv);

        if (rows.IsFailure)
        {
            return rows.Error;
        }

        var locale = SupportedLocale.OrDefault(access.Locale);
        var now = timeProvider.GetDatabaseUtcNow();
        var accounts = new List<(MemberCsvRow Row, User Account)>();

        foreach (var row in rows.Value)
        {
            if (!SchoolRoleNames.All.Contains(row.Role) || !SchoolRoleNames.FitsKind(row.Role, access.Kind))
            {
                return ForRow(MemberErrors.RoleUnknown, row);
            }

            var account = User.CreateSchoolAccount(
                command.OrganizationId,
                row.DisplayName,
                row.Username,
                row.Email,
                locale,
                now.AddMilliseconds(row.RowNumber));

            if (account.IsFailure)
            {
                return ForRow(account.Error, row);
            }

            accounts.Add((row, account.Value));
        }

        if (FindRepeated(accounts, account => account.OrganizationUsername) is { } repeatedUsername)
        {
            return ForRow(MemberImportErrors.UsernameRepeated with { Field = "Username" }, repeatedUsername);
        }

        if (FindRepeated(accounts, account => account.Email?.ToUpperInvariant()) is { } repeatedEmail)
        {
            return ForRow(MemberImportErrors.EmailRepeated with { Field = "Email" }, repeatedEmail);
        }

        var taken = await context.ListTakenUsernamesAsync(
            command.OrganizationId,
            [.. accounts.Select(imported => imported.Account.OrganizationUsername!)],
            cancellationToken);

        if (accounts.FirstOrDefault(imported => taken.Contains(imported.Account.OrganizationUsername!)) is
            { Row: { } takenRow })
        {
            return ForRow(MemberErrors.UsernameInUse, takenRow);
        }

        return accounts;
    }

    private async Task<Result<IReadOnlyList<SchoolMember>>> ImportAsync(
        ImportMembersCommand command,
        IReadOnlyList<(MemberCsvRow Row, User Account)> accounts,
        List<MemberCredential> credentials,
        DateTimeOffset now)
    {
        var firstAccountId = accounts[0].Account.Id;
        var alreadyImported = await context.FindMemberAsync(
            command.OrganizationId,
            null,
            firstAccountId,
            now,
            CancellationToken.None);

        if (alreadyImported is null)
        {
            context.ChangeTracker.Clear();

            for (var index = 0; index < accounts.Count; index++)
            {
                var (row, account) = accounts[index];
                var created = await CreateAsync(command, row, account, credentials[index]);

                if (created.IsFailure)
                {
                    return ForRow(created.Error, row);
                }
            }
        }

        var membersByAccountId = (await context.FindMembersAsync(
                command.OrganizationId,
                [.. accounts.Select(imported => imported.Account.Id)],
                now,
                CancellationToken.None))
            .ToDictionary(member => member.AccountId);

        return accounts
            .Select(imported => membersByAccountId.GetValueOrDefault(imported.Account.Id)
                ?? throw new InvalidOperationException("An imported member cannot be read back."))
            .ToList();
    }

    private async Task<Result> CreateAsync(
        ImportMembersCommand command,
        MemberCsvRow row,
        User account,
        MemberCredential credential)
    {
        try
        {
            var saved = await userManager.CreateAsync(account);

            if (!saved.Succeeded)
            {
                return Result.Failure(saved.Errors.First().ToError());
            }
        }
        catch (DbUpdateException ex) when (MemberConflicts.Of(ex) is { } conflict)
        {
            return Result.Failure(conflict);
        }

        if (await context.GrantMemberRoleAsync(account.Id, command.OrganizationId, row.Role) != 1
            || !await memberCredentials.ApplyAsync(
                credential,
                account.Id,
                command.OrganizationId,
                command.ActorId,
                command.ActorSessionId))
        {
            throw new InvalidOperationException("An imported member's role or credential was not saved.");
        }

        await context.RecordOrganizationChangeAsync(
            command.OrganizationId,
            OrganizationChangeNames.AccountCreated,
            account.Id,
            row.Role,
            command.ActorId,
            command.ActorSessionId);

        return Result.Success();
    }

    private static MemberCsvRow? FindRepeated(
        IReadOnlyList<(MemberCsvRow Row, User Account)> accounts,
        Func<User, string?> key) =>
        accounts
            .Where(imported => key(imported.Account) is not null)
            .GroupBy(imported => key(imported.Account))
            .FirstOrDefault(group => group.Count() > 1)
            ?.Skip(1)
            .First()
            .Row;

    private static Error ForRow(Error error, MemberCsvRow row) => error with
    {
        Field = error.Field is { Length: > 0 } field
            ? $"rows[{row.RowNumber}].{char.ToLowerInvariant(field[0])}{field[1..]}"
            : $"rows[{row.RowNumber}]"
    };
}

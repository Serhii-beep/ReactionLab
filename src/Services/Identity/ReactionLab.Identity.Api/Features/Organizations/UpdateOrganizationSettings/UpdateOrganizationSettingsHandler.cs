using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactionLab.Identity.Api.Features.Members;
using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations.UpdateOrganizationSettings;

internal sealed class UpdateOrganizationSettingsHandler(
    SchoolAdminTransaction adminTransaction,
    AppDbContext context,
    ILogger<UpdateOrganizationSettingsHandler> logger)
    : ICommandHandler<UpdateOrganizationSettingsCommand, OrganizationSettingsResponse>
{
    private const string SignInCodeIndex = "ix_organizations_sign_in_code";

    public async ValueTask<Result<OrganizationSettingsResponse>> HandleAsync(
        UpdateOrganizationSettingsCommand command,
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
            return Result.Failure<OrganizationSettingsResponse>(access.Error);
        }

        if (command is { Name: null, Locale: null, TimeZone: null, SignInCode: null }
            && command.TeachersMayResetStudentPasswords is null)
        {
            return Result.Failure<OrganizationSettingsResponse>(OrganizationErrors.SettingsRequired);
        }

        if (await context.FindOrganizationAsync(command.OrganizationId, cancellationToken) is not { } preview)
        {
            return Result.Failure<OrganizationSettingsResponse>(OrganizationErrors.NotFound);
        }

        var signInCode = preview.SignInCode;
        var changed = Apply(preview, command);

        if (changed.IsFailure)
        {
            return Result.Failure<OrganizationSettingsResponse>(changed.Error);
        }

        if (preview.SignInCode != signInCode
            && await context.IsSignInCodeTakenAsync(preview.SignInCode, cancellationToken))
        {
            return Result.Failure<OrganizationSettingsResponse>(OrganizationErrors.SignInCodeInUse);
        }

        var updated = await adminTransaction.RunAsync(
            command.ActorId,
            command.ActorSessionId,
            command.OrganizationId,
            SchoolAdminRules.Admin,
            [],
            changedAccountId: null,
            _ => UpdateAsync(command));

        if (updated.IsFailure)
        {
            return Result.Failure<OrganizationSettingsResponse>(updated.Error);
        }

        logger.LogInformation(
            "Account {ActorId} changed the settings of organization {OrganizationId} in session {SessionId}.",
            command.ActorId,
            command.OrganizationId,
            command.ActorSessionId);

        return Result.Success(OrganizationSettingsResponse.From(updated.Value));
    }

    private async Task<Result<Organization>> UpdateAsync(UpdateOrganizationSettingsCommand command)
    {
        context.ChangeTracker.Clear();

        var organization = await context.Set<Organization>()
            .SingleAsync(found => found.Id == command.OrganizationId, CancellationToken.None);

        var changed = Apply(organization, command);

        if (changed.IsFailure)
        {
            return changed.Error;
        }

        try
        {
            await context.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: SignInCodeIndex
        })
        {
            return OrganizationErrors.SignInCodeInUse;
        }

        foreach (var change in changed.Value)
        {
            await context.RecordOrganizationChangeAsync(
                command.OrganizationId,
                change,
                accountId: null,
                role: null,
                command.ActorId,
                command.ActorSessionId);
        }

        return organization;
    }

    private static Result<IReadOnlyList<string>> Apply(
        Organization organization,
        UpdateOrganizationSettingsCommand command) =>
        organization.ChangeSettings(
            command.Name,
            command.Locale,
            command.TimeZone,
            command.SignInCode,
            command.TeachersMayResetStudentPasswords);
}

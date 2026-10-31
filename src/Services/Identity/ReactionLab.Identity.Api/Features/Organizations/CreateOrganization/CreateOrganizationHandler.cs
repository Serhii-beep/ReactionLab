using Microsoft.AspNetCore.Identity;
using ReactionLab.Identity.Api.Features.Accounts;
using ReactionLab.Identity.Api.Features.AccountSetups;
using ReactionLab.Identity.Api.Features.Organizations.Contracts;
using ReactionLab.Identity.Api.Features.PlatformRoles;
using ReactionLab.Identity.Api.Features.Sessions;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.AccountSetups;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Api.Features.Organizations.CreateOrganization;

internal sealed class CreateOrganizationHandler(
    RecentSignInCheck recentSignInCheck,
    PlatformAdminTransaction adminTransaction,
    UserManager<User> userManager,
    AppDbContext context,
    TimeProvider timeProvider,
    ILogger<CreateOrganizationHandler> logger)
    : ICommandHandler<CreateOrganizationCommand, CreatedOrganizationResponse>
{
    private const int SignInCodeDraws = 10;
    private const string FirstAdminField = "firstAdmin";

    public async ValueTask<Result<CreatedOrganizationResponse>> HandleAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetDatabaseUtcNow();
        var organization = Organization.Create(command.Kind, command.DistrictId, command.Name, now);

        if (organization.IsFailure)
        {
            return Result.Failure<CreatedOrganizationResponse>(organization.Error);
        }

        var locale = SupportedLocale.Create(command.FirstAdminLocale);

        if (locale.IsFailure)
        {
            return Result.Failure<CreatedOrganizationResponse>(ForFirstAdmin(locale.Error));
        }

        var firstAdmin = User.CreateSchoolAccount(
            organization.Value.Id,
            command.FirstAdminDisplayName,
            command.FirstAdminUsername,
            command.FirstAdminEmail,
            locale.Value,
            now);

        if (firstAdmin.IsFailure)
        {
            return Result.Failure<CreatedOrganizationResponse>(ForFirstAdmin(firstAdmin.Error));
        }

        var proof = await recentSignInCheck.CheckConfirmedPasswordAsync(
            command.AdminId,
            command.AdminSessionId,
            cancellationToken);

        if (proof.IsFailure)
        {
            logger.LogInformation(
                "Session {SessionId} may not create an organization: {ErrorCode}.",
                command.AdminSessionId,
                proof.Error.Code);

            return Result.Failure<CreatedOrganizationResponse>(proof.Error);
        }

        var (setupToken, setupTokenHash) = AccountSetupTokens.Issue();
        var setupExpiresAt = now + AccountSetup.Lifetime;

        var created = await adminTransaction.RunAsync(
            command.AdminId,
            command.AdminSessionId,
            () => CreateAsync(command, organization.Value, firstAdmin.Value, setupTokenHash, now, setupExpiresAt));

        if (created.IsFailure)
        {
            return Result.Failure<CreatedOrganizationResponse>(created.Error);
        }

        logger.LogInformation(
            "Admin {AdminId} created {Kind} {OrganizationId}, first admin {AccountId}, in session {SessionId}.",
            command.AdminId,
            created.Value.Kind,
            created.Value.Id,
            firstAdmin.Value.Id,
            command.AdminSessionId);

        return Result.Success(new CreatedOrganizationResponse(
            OrganizationResponse.From(created.Value),
            new AccountSetupLinkResponse(
                firstAdmin.Value.Id,
                firstAdmin.Value.OrganizationUsername!,
                setupToken,
                setupExpiresAt)));
    }

    private async Task<Result<Organization>> CreateAsync(
        CreateOrganizationCommand command,
        Organization organization,
        User firstAdmin,
        byte[] setupTokenHash,
        DateTimeOffset now,
        DateTimeOffset setupExpiresAt)
    {
        if (await context.FindOrganizationAsync(organization.Id, CancellationToken.None) is not null)
        {
            return Result.Success(organization);
        }

        if (organization.DistrictId is { } districtId
            && !await context.IsDistrictAsync(districtId, CancellationToken.None))
        {
            return Result.Failure<Organization>(OrganizationErrors.DistrictNotFound);
        }

        if (!await DrawFreeSignInCodeAsync(organization))
        {
            return Result.Failure<Organization>(OrganizationErrors.SignInCodeUnavailable);
        }

        context.ChangeTracker.Clear();
        context.Add(organization);

        var saved = await SaveFirstAdminAsync(firstAdmin);

        if (saved.IsFailure)
        {
            return Result.Failure<Organization>(saved.Error);
        }

        var role = SchoolRoleNames.AdminOf(organization.Kind);
        var granted = await context.GrantSchoolRoleAsync(firstAdmin.Id, organization.Id, role);
        var issued = await context.IssueAccountSetupAsync(
            organization.Id,
            firstAdmin.Id,
            setupTokenHash,
            now,
            setupExpiresAt,
            command.AdminId,
            command.AdminSessionId);

        if (granted != 1 || issued != 1)
        {
            throw new InvalidOperationException("The first admin's role or setup link was not saved.");
        }

        await context.RecordOrganizationChangeAsync(
            organization.Id,
            OrganizationChangeNames.Created,
            accountId: null,
            role: null,
            command.AdminId,
            command.AdminSessionId);

        await context.RecordOrganizationChangeAsync(
            organization.Id,
            OrganizationChangeNames.AccountCreated,
            firstAdmin.Id,
            role,
            command.AdminId,
            command.AdminSessionId);

        return Result.Success(organization);
    }

    private async Task<Result> SaveFirstAdminAsync(User firstAdmin)
    {
        var saved = await userManager.CreateAsync(firstAdmin);

        return saved.Succeeded ? Result.Success() : Result.Failure(ForFirstAdmin(saved.Errors.First().ToError()));
    }

    private async Task<bool> DrawFreeSignInCodeAsync(Organization organization)
    {
        for (var draw = 0; draw < SignInCodeDraws; draw++)
        {
            if (!await context.IsSignInCodeTakenAsync(organization.SignInCode, CancellationToken.None))
            {
                return true;
            }

            organization.ReplaceSignInCode();
        }

        return false;
    }

    private static Error ForFirstAdmin(Error error) => error with
    {
        Field = error.Field is { Length: > 0 } field
            ? $"{FirstAdminField}.{char.ToLowerInvariant(field[0])}{field[1..]}"
            : FirstAdminField
    };
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReactionLab.Identity.Persistence;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.Identity.Persistence.Organizations;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Migrator;

internal sealed class DemoDistrictSeeding(
    AppDbContext context,
    UserManager<User> userManager,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<DemoDistrictSeeding> logger)
{
    public const string DemoPasswordKey = "Identity:DemoPassword";

    private const string DistrictSignInCode = "DEMODISTRICT";
    private const string SchoolSignInCode = "DEMOSCHOOL";

    private static readonly (string Username, string DisplayName, string Role)[] SchoolAccounts =
    [
        ("principal", "Demo Principal", SchoolRoleNames.SchoolAdmin),
        ("teacher", "Demo Teacher", SchoolRoleNames.Teacher),
        ("student1", "Demo Student 1", SchoolRoleNames.Student),
        ("student2", "Demo Student 2", SchoolRoleNames.Student),
        ("student3", "Demo Student 3", SchoolRoleNames.Student)
    ];

    public Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || configuration[DemoPasswordKey] is not { Length: > 0 } password)
        {
            return Task.CompletedTask;
        }

        return context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            if (await context.IsSignInCodeTakenAsync(DistrictSignInCode, cancellationToken)
                || await context.IsSignInCodeTakenAsync(SchoolSignInCode, cancellationToken))
            {
                logger.LogInformation(
                    "The demo district signs in with code {DistrictSignInCode}, and its school with "
                    + "{SchoolSignInCode}.",
                    DistrictSignInCode,
                    SchoolSignInCode);
                return;
            }

            var now = TimeProvider.System.GetDatabaseUtcNow();
            var district = CreateOrganization(
                OrganizationKinds.District,
                districtId: null,
                "Demo District",
                DistrictSignInCode,
                now);
            var school = CreateOrganization(
                OrganizationKinds.School,
                district.Id,
                "Demo School",
                SchoolSignInCode,
                now);

            context.AddRange(district, school);
            await context.SaveChangesAsync(cancellationToken);

            await RecordCreatedAsync(district);
            await CreateAccountAsync(
                district,
                "superintendent",
                "Demo Superintendent",
                SchoolRoleNames.DistrictAdmin,
                password,
                now);
            await RecordCreatedAsync(school);

            foreach (var (username, displayName, role) in SchoolAccounts)
            {
                await CreateAccountAsync(school, username, displayName, role, password, now);
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Seeded the demo district, which signs in with code {DistrictSignInCode}, and its school, "
                + "{SchoolSignInCode}. Every demo account's password is {Setting}.",
                DistrictSignInCode,
                SchoolSignInCode,
                DemoPasswordKey);
        });
    }

    private Task<int> RecordCreatedAsync(Organization organization) =>
        context.RecordOrganizationChangeAsync(
            organization.Id,
            OrganizationChangeNames.Created,
            accountId: null,
            role: null,
            changedBy: null,
            changedInSession: null);

    private async Task CreateAccountAsync(
        Organization organization,
        string username,
        string displayName,
        string role,
        string password,
        DateTimeOffset now)
    {
        var account = User.CreateSchoolAccount(
            organization.Id,
            displayName,
            username,
            email: null,
            SupportedLocale.Default,
            now).Value;
        account.PasswordHash = userManager.PasswordHasher.HashPassword(account, password);

        var created = await userManager.CreateAsync(account);

        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"The demo account '{username}' was not created: {created.Errors.First().Code}.");
        }

        var granted = await context.GrantSchoolRoleAsync(account.Id, organization.Id, role);

        if (granted != 1)
        {
            throw new InvalidOperationException($"The demo account '{username}' was not given its role.");
        }

        await context.RecordOrganizationChangeAsync(
            organization.Id,
            OrganizationChangeNames.AccountCreated,
            account.Id,
            role,
            changedBy: null,
            changedInSession: null);
    }

    private static Organization CreateOrganization(
        string kind,
        Guid? districtId,
        string name,
        string signInCode,
        DateTimeOffset now)
    {
        var organization = Organization.Create(kind, districtId, name, now).Value;

        if (organization.ChangeSignInCode(signInCode).IsFailure)
        {
            throw new InvalidOperationException($"'{signInCode}' is not a sign-in code.");
        }

        return organization;
    }
}

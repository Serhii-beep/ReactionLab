using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Notifications.Templates;

namespace ReactionLab.Notifications.ServiceCalls;

internal sealed class MailDetailDirectory(Contacts.ContactsClient contacts, IdentityCalls identity)
{
    public async Task<MailDetail?> FindAsync(
        MailDetailKind kind,
        Guid recipientId,
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var accountId = recipientId.ToString();
        var subjectId = jobId.ToString();

        return kind switch
        {
            MailDetailKind.PasswordChange => Describe(await identity.CallAsync(
                options => contacts.GetPasswordChangeAsync(
                    new GetPasswordChangeRequest { AccountId = accountId, ChangeId = subjectId },
                    options),
                cancellationToken)),
            MailDetailKind.SignInMethodChange => Describe(await identity.CallAsync(
                options => contacts.GetSignInMethodChangeAsync(
                    new GetSignInMethodChangeRequest { AccountId = accountId, ChangeId = subjectId },
                    options),
                cancellationToken)),
            MailDetailKind.SignInLockout => DescribeSignInLockout(await identity.CallAsync(
                options => contacts.GetLockoutAsync(new GetLockoutRequest { AccountId = accountId }, options),
                cancellationToken)),
            MailDetailKind.PasswordCheckLockout => DescribePasswordCheckLockout(await identity.CallAsync(
                options => contacts.GetLockoutAsync(new GetLockoutRequest { AccountId = accountId }, options),
                cancellationToken)),
            MailDetailKind.PlatformRoleChange => Describe(await identity.CallAsync(
                options => contacts.GetPlatformRoleChangeAsync(
                    new GetPlatformRoleChangeRequest { AccountId = accountId, ChangeId = subjectId },
                    options),
                cancellationToken)),
            MailDetailKind.SchoolAdminChange => Describe(await identity.CallAsync(
                options => contacts.GetOrganizationChangeAsync(
                    new GetOrganizationChangeRequest { AccountId = accountId, ChangeId = subjectId },
                    options),
                cancellationToken)),
            MailDetailKind.AccountSetup => Describe(await identity.CallAsync(
                options => contacts.GetAccountSetupAsync(
                    new GetAccountSetupRequest { AccountId = accountId, RequestId = subjectId },
                    options),
                cancellationToken)),
            _ => null
        };
    }

    private static RecordedPasswordChange? Describe(PasswordChangeDetails? details) =>
        details is null
            ? null
            : new RecordedPasswordChange(
                details.ChangedAt.ToDateTimeOffset(),
                details.Browser,
                details.OperatingSystem);

    private static RecordedSignInMethodChange? Describe(SignInMethodChangeDetails? details) =>
        details is null
            ? null
            : new RecordedSignInMethodChange(
                details.ChangedAt.ToDateTimeOffset(),
                details.Change,
                details.ProviderDisplayName,
                details.Browser,
                details.OperatingSystem,
                details.HasPassword,
                [.. details.ProviderDisplayNames]);

    private static CurrentLockout? DescribeSignInLockout(LockoutDetails? details) =>
        details switch
        {
            null => null,
            { SignInDisabled: true } => new CurrentLockout(null, Disabled: true, details.SignInFailures),
            { SignInPausedUntil: { } pausedUntil } =>
                new CurrentLockout(pausedUntil.ToDateTimeOffset(), Disabled: false, details.SignInFailures),
            _ => null
        };

    private static CurrentLockout? DescribePasswordCheckLockout(LockoutDetails? details) =>
        details switch
        {
            null => null,
            { PasswordChecksDisabled: true } => new CurrentLockout(null, Disabled: true, details.PasswordCheckFailures),
            { PasswordChecksPausedUntil: { } pausedUntil } =>
                new CurrentLockout(pausedUntil.ToDateTimeOffset(), Disabled: false, details.PasswordCheckFailures),
            _ => null
        };

    private static RecordedPlatformRoleChange? Describe(PlatformRoleChangeDetails? details) =>
        details is null
            ? null
            : new RecordedPlatformRoleChange(
                details.ChangedAt.ToDateTimeOffset(),
                details.Granted,
                details.Source,
                details.AccountDisplayName,
                details.AccountEmail,
                details.ChangedByDisplayName,
                details.ChangedByEmail);

    private static RecordedSchoolAdminChange? Describe(OrganizationChangeDetails? details) =>
        details is null
            ? null
            : new RecordedSchoolAdminChange(
                details.ChangedAt.ToDateTimeOffset(),
                details.OrganizationName,
                details.Change,
                details.Role,
                details.AccountDisplayName,
                details.AccountUsername,
                details.ChangedByDisplayName,
                details.ChangedByPlatform,
                details.AccountCount);

    private static MailedAccountSetup? Describe(AccountSetupDetails? details) =>
        details is null
            ? null
            : new MailedAccountSetup(
                details.Purpose,
                details.OrganizationName,
                details.SignInCode,
                details.Username,
                [.. details.Roles],
                details.RequestedByDisplayName,
                details.RequestedByPlatformAdmin,
                [.. details.RequestedByRoles]);
}

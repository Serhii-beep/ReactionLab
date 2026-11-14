using ReactionLab.Identity.Contracts.Contacts.V1;
using ReactionLab.Notifications.Persistence.Mail;

namespace ReactionLab.Notifications.Templates;

internal static class MailDetailSamples
{
    private const string Provider = "Google";
    private const string Organization = "Sample School";
    private const string SignInCode = "SAMPLE";

    private static readonly TimeSpan SampleLockout = TimeSpan.FromMinutes(5);

    public static IReadOnlyList<MailDetail?> For(IMailTemplate template, DateTimeOffset now) =>
        template.MailType switch
        {
            MailTypes.PasswordChanged =>
            [
                new RecordedPasswordChange(now, "Chrome", "Windows"),
                new RecordedPasswordChange(now, string.Empty, string.Empty)
            ],
            MailTypes.SignInMethodChanged =>
            [
                new RecordedSignInMethodChange(
                    now,
                    SignInMethodChangeKind.ProviderConnected,
                    Provider,
                    "Safari",
                    "iOS",
                    HasPassword: true,
                    [Provider]),
                new RecordedSignInMethodChange(
                    now,
                    SignInMethodChangeKind.ProviderDisconnected,
                    Provider,
                    string.Empty,
                    string.Empty,
                    HasPassword: true,
                    []),
                new RecordedSignInMethodChange(
                    now,
                    SignInMethodChangeKind.PasswordSet,
                    string.Empty,
                    "Chrome",
                    "Windows",
                    HasPassword: true,
                    [Provider]),
                new RecordedSignInMethodChange(
                    now,
                    SignInMethodChangeKind.PasswordRemoved,
                    string.Empty,
                    "Firefox",
                    string.Empty,
                    HasPassword: false,
                    [Provider]),
                new RecordedSignInMethodChange(
                    now,
                    SignInMethodChangeKind.AccountTakenOver,
                    Provider,
                    "Chrome",
                    "Android",
                    HasPassword: false,
                    []),
            ],
            MailTypes.SignInLocked or MailTypes.PasswordChecksLocked =>
            [
                new CurrentLockout(now + SampleLockout, Disabled: false, 10),
                new CurrentLockout(null, Disabled: true, 100)
            ],
            MailTypes.PlatformAdminChanged =>
            [
                new RecordedPlatformRoleChange(
                    now,
                    Granted: true,
                    PlatformRoleChangeSource.Admin,
                    "Sample Admin",
                    "admin@example.com",
                    "Sample Owner",
                    "owner@example.com"),
                new RecordedPlatformRoleChange(
                    now,
                    Granted: false,
                    PlatformRoleChangeSource.Admin,
                    "Sample Admin",
                    string.Empty,
                    "Sample Owner",
                    string.Empty),
                new RecordedPlatformRoleChange(
                    now,
                    Granted: true,
                    PlatformRoleChangeSource.FirstAdmin,
                    "Sample Admin",
                    "admin@example.com",
                    string.Empty,
                    string.Empty),
                new RecordedPlatformRoleChange(
                    now,
                    Granted: true,
                    PlatformRoleChangeSource.BreakGlass,
                    "Sample Admin",
                    "admin@example.com",
                    string.Empty,
                    string.Empty)
            ],
            MailTypes.SchoolAdminChanged =>
            [
                new RecordedSchoolAdminChange(
                    now,
                    Organization,
                    AdminRoleChange.Granted,
                    "school-admin",
                    "Sample Teacher",
                    "teacher",
                    "Sample Principal",
                    ChangedByPlatform: false,
                    1),
                new RecordedSchoolAdminChange(
                    now,
                    Organization,
                    AdminRoleChange.Granted,
                    "school-admin",
                    "Sample Teacher",
                    "teacher",
                    "Sample Owner",
                    ChangedByPlatform: true,
                    30),
                new RecordedSchoolAdminChange(
                    now,
                    Organization,
                    AdminRoleChange.Revoked,
                    "district-admin",
                    "Sample Teacher",
                    string.Empty,
                    "Sample Principal",
                    ChangedByPlatform: false,
                    1),
                new RecordedSchoolAdminChange(
                    now,
                    Organization,
                    AdminRoleChange.AccountDeleted,
                    "school-admin",
                    string.Empty,
                    string.Empty,
                    "Sample Principal",
                    ChangedByPlatform: false,
                    1)
            ],
            MailTypes.SchoolInvitation =>
            [
                new MailedAccountSetup(
                    AccountSetupPurpose.Invitation,
                    Organization,
                    SignInCode,
                    string.Empty,
                    ["teacher"],
                    "Sample Principal",
                    RequestedByPlatformAdmin: false,
                    ["school-admin"]),
                new MailedAccountSetup(
                    AccountSetupPurpose.Invitation,
                    Organization,
                    SignInCode,
                    "sample",
                    [],
                    string.Empty,
                    RequestedByPlatformAdmin: false,
                    []),
                new MailedAccountSetup(
                    AccountSetupPurpose.Invitation,
                    Organization,
                    SignInCode,
                    "principal",
                    ["school-admin"],
                    "Sample Owner",
                    RequestedByPlatformAdmin: true,
                    [])
            ],
            MailTypes.SchoolPasswordReset =>
            [
                new MailedAccountSetup(
                    AccountSetupPurpose.AdminReset,
                    Organization,
                    SignInCode,
                    "student",
                    ["student"],
                    "Sample Principal",
                    RequestedByPlatformAdmin: false,
                    ["school-admin"]),
                new MailedAccountSetup(
                    AccountSetupPurpose.AdminReset,
                    Organization,
                    SignInCode,
                    "student",
                    ["student"],
                    "Sample Teacher",
                    RequestedByPlatformAdmin: false,
                    ["teacher"]),
                new MailedAccountSetup(
                    AccountSetupPurpose.AdminReset,
                    Organization,
                    SignInCode,
                    "student",
                    ["student"],
                    string.Empty,
                    RequestedByPlatformAdmin: false,
                    []),
                new MailedAccountSetup(
                    AccountSetupPurpose.AdminReset,
                    Organization,
                    SignInCode,
                    "principal",
                    ["school-admin"],
                    "Sample Owner",
                    RequestedByPlatformAdmin: true,
                    []),
                new MailedAccountSetup(
                    AccountSetupPurpose.SelfReset,
                    Organization,
                    SignInCode,
                    string.Empty,
                    ["teacher"],
                    "Sample Teacher",
                    RequestedByPlatformAdmin: false,
                    [])
            ],
            _ => [null]
        };
}

namespace ReactionLab.Notifications.Persistence.Mail;

public static class MailTypes
{
    public const string RepeatedRegistration = "repeated-registration";
    public const string EmailConfirmation = "email-confirmation";
    public const string PasswordReset = "password-reset";
    public const string PasswordChanged = "password-changed";
    public const string SignInMethodChanged = "sign-in-method-changed";
    public const string SignInLocked = "sign-in-locked";
    public const string PasswordChecksLocked = "password-checks-locked";
    public const string PlatformAdminChanged = "platform-admin-changed";
    public const string SchoolAdminChanged = "school-admin-changed";
    public const string SchoolInvitation = "school-invitation";
    public const string SchoolPasswordReset = "school-password-reset";

    public static bool IsUrgent(string mailType) =>
        mailType is EmailConfirmation or PasswordReset or SchoolInvitation or SchoolPasswordReset;
}

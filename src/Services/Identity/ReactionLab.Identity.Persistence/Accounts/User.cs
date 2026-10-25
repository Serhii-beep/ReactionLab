using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Persistence.Accounts;

public sealed partial class User : IdentityUser<Guid>
{
    public const int MaximumEmailLength = 256;
    public const int MaximumDisplayNameLength = 64;
    public const int MaximumUsernameLength = 64;
    public const int MaximumLocaleLength = 10;
    public const int MaximumLoginKeyLength = 128;
    public const int MinimumSelfRegistrationAge = 13;
    public const int EarliestBirthYear = 1900;

    public static readonly Error EmailRequired = Error.Validation(
        "Account.EmailRequired",
        "An email address is required.",
        nameof(Email));

    public static readonly Error EmailTooLong = Error.Validation(
        "Account.EmailTooLong",
        $"An email address must not exceed {MaximumEmailLength} characters.",
        nameof(Email))
        .WithArgs(("max", MaximumEmailLength));

    public static readonly Error EmailInvalid = Error.Validation(
        "Account.EmailInvalid",
        "The email address is not valid.",
        nameof(Email));

    public static readonly Error DisplayNameRequired = Error.Validation(
        "Account.DisplayNameRequired",
        "A display name is required.",
        nameof(DisplayName));

    public static readonly Error DisplayNameTooLong = Error.Validation(
        "Account.DisplayNameTooLong",
        $"A display name must not exceed {MaximumDisplayNameLength} characters.",
        nameof(DisplayName))
        .WithArgs(("max", MaximumDisplayNameLength));

    public static readonly Error DisplayNameInvalid = Error.Validation(
        "Account.DisplayNameInvalid",
        "A display name needs a letter, digit or symbol, and no invisible, text-direction or stacked characters.",
        nameof(DisplayName));

    public static readonly Error UsernameRequired = Error.Validation(
        "Account.UsernameRequired",
        "A username is required.",
        UsernameField);

    public static readonly Error UsernameTooLong = Error.Validation(
        "Account.UsernameTooLong",
        $"A username must not exceed {MaximumUsernameLength} characters.",
        UsernameField)
        .WithArgs(("max", MaximumUsernameLength));

    public static readonly Error UsernameInvalid = Error.Validation(
        "Account.UsernameInvalid",
        "A username uses only a to z, digits, dots, hyphens, and underscores, and starts with a letter or digit.",
        UsernameField);

    public static readonly Error BirthYearInvalid = Error.Validation(
        "Account.BirthYearInvalid",
        "The birth year is not a valid year.",
        nameof(BirthYear));

    public static readonly Error SchoolAccountRequired = Error.Validation(
        "Account.SchoolAccountRequired",
        "This account can only be created by a school.");

    private const string UsernameField = "Username";

    private static readonly EmailAddressAttribute EmailFormat = new();

    private User()
    {

    }

    public string DisplayName { get; private set; } = string.Empty;

    public string PreferredLocale { get; private set; } = string.Empty;

    public int? BirthYear { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public int CurrentPasswordFailures { get; private set; }

    public DateTimeOffset? CurrentPasswordLockoutEnd { get; private set; }

    public DateTimeOffset? PasswordProbationEndsAt { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public string? OrganizationUsername { get; private set; }

    public static Result<User> Register(
        string? email,
        string? displayName,
        SupportedLocale locale,
        int? birthYear,
        DateTimeOffset now)
    {
        var address = NormalizeEmail(email);

        if (address.IsFailure)
        {
            return address.Error;
        }

        return Create(address.Value, emailConfirmed: false, displayName, locale, birthYear, now);
    }

    public static Result<User> RegisterExternal(
        string? verifiedEmail,
        string? displayName,
        SupportedLocale locale,
        int? birthYear,
        DateTimeOffset now)
    {
        if (verifiedEmail is null)
        {
            return Create(null, emailConfirmed: false, displayName, locale, birthYear, now);
        }

        var address = NormalizeEmail(verifiedEmail);

        if (address.IsFailure)
        {
            return address.Error;
        }

        return Create(address.Value, emailConfirmed: true, displayName, locale, birthYear, now);
    }

    public static Result<User> CreateSchoolAccount(
        Guid organizationId,
        string? displayName,
        string? username,
        string? email,
        SupportedLocale locale,
        DateTimeOffset now)
    {
        var name = NormalizeDisplayName(displayName);

        if (name.IsFailure)
        {
            return name.Error;
        }

        var organizationUsername = NormalizeOrganizationUsername(username);

        if (organizationUsername.IsFailure)
        {
            return organizationUsername.Error;
        }

        var address = string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email);

        if (address is { IsFailure: true })
        {
            return address.Error;
        }

        var id = Guid.CreateVersion7(now);

        return new User
        {
            Id = id,
            UserName = id.ToString(),
            Email = address?.Value,
            DisplayName = name.Value,
            PreferredLocale = locale.Code,
            OrganizationId = organizationId,
            OrganizationUsername = organizationUsername.Value,
            CreatedAt = now
        };
    }

    public static Result<string> NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return EmailRequired;
        }

        if (!UnicodeText.TryNormalize(email.Trim(), out var normalized)
            || normalized.Any(char.IsControl)
            || normalized.Any(UpperCasesIntoAscii))
        {
            return EmailInvalid;
        }

        if (normalized.Length > MaximumEmailLength)
        {
            return EmailTooLong;
        }

        return EmailFormat.IsValid(normalized) ? normalized : EmailInvalid;
    }

    public static Result<string> NormalizeDisplayName(string? displayName) =>
        DisplayNames.Normalize(
            displayName,
            MaximumDisplayNameLength,
            DisplayNameRequired,
            DisplayNameTooLong,
            DisplayNameInvalid);

    public static Result<string> NormalizeOrganizationUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return UsernameRequired;
        }

        var trimmed = username.Trim();

        if (!trimmed.All(char.IsAscii))
        {
            return UsernameInvalid;
        }

        if (trimmed.Length > MaximumUsernameLength)
        {
            return UsernameTooLong;
        }

        var lowered = trimmed.ToLowerInvariant();

        return UsernamePattern().IsMatch(lowered) ? lowered : UsernameInvalid;
    }

    private static Result<User> Create(
        string? email,
        bool emailConfirmed,
        string? displayName,
        SupportedLocale locale,
        int? birthYear,
        DateTimeOffset now)
    {
        var name = NormalizeDisplayName(displayName);

        if (name.IsFailure)
        {
            return name.Error;
        }

        var eligibility = CheckSelfRegistrationAge(birthYear, now);

        if (eligibility.IsFailure)
        {
            return eligibility.Error;
        }

        var id = Guid.CreateVersion7(now);

        return new User
        {
            Id = id,
            UserName = email ?? id.ToString(),
            Email = email,
            EmailConfirmed = emailConfirmed,
            DisplayName = name.Value,
            PreferredLocale = locale.Code,
            BirthYear = birthYear,
            CreatedAt = now
        };
    }

    private static Result CheckSelfRegistrationAge(int? birthYear, DateTimeOffset now)
    {
        if (birthYear is not { } year || year < EarliestBirthYear || year > now.Year)
        {
            return Result.Failure(BirthYearInvalid);
        }

        var youngestPossibleAge = now.Year - year - 1;

        return youngestPossibleAge >= MinimumSelfRegistrationAge
            ? Result.Success()
            : Result.Failure(SchoolAccountRequired);
    }

    private static bool UpperCasesIntoAscii(char character) =>
        !char.IsAscii(character) && char.IsAscii(char.ToUpperInvariant(character));

    [GeneratedRegex(@"^[a-z0-9][a-z0-9._-]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex UsernamePattern();
}

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Persistence.Accounts;

public sealed partial class User : IdentityUser<Guid>
{
    public const int MaximumEmailLength = 256;
    public const int MaximumDisplayNameLength = 64;
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

    public static readonly Error BirthYearInvalid = Error.Validation(
        "Account.BirthYearInvalid",
        "The birth year is not a valid year.",
        nameof(BirthYear));

    public static readonly Error SchoolAccountRequired = Error.Validation(
        "Account.SchoolAccountRequired",
        "This account can only be created by a school.");

    private const int ZeroWidthNonJoiner = 0x200C;
    private const int ZeroWidthJoiner = 0x200D;
    private const int MaximumCombiningMarksInARow = 4;

    private static readonly EmailAddressAttribute EmailFormat = new();
    private static readonly int[] BlankLookingCharacters =
        [0x034F, 0x115F, 0x1160, 0x17B4, 0x17B5, 0x2800, 0x3164, 0xFFA0];

    private User()
    {

    }

    public string DisplayName { get; private set; } = string.Empty;

    public string PreferredLocale { get; private set; } = string.Empty;

    public int? BirthYear { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

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

    public static Result<string> NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return EmailRequired;
        }

        if (!UnicodeText.TryNormalize(email.Trim(), out var normalized) || normalized.Any(char.IsControl))
        {
            return EmailInvalid;
        }

        if (normalized.Length > MaximumEmailLength)
        {
            return EmailTooLong;
        }

        return EmailFormat.IsValid(normalized) ? normalized : EmailInvalid;
    }

    public static Result<string> NormalizeDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return DisplayNameRequired;
        }

        if (!UnicodeText.TryNormalize(WhitespaceRun().Replace(displayName.Trim(), " "), out var normalized))
        {
            return DisplayNameInvalid;
        }

        var characters = normalized.EnumerateRunes().ToList();

        if (characters.Count > MaximumDisplayNameLength)
        {
            return DisplayNameTooLong;
        }

        return characters.Any(IsHiddenOrUnassigned) || !characters.Any(IsVisibleSign) || HasStackedMarks(characters)
            ? DisplayNameInvalid
            : normalized;
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

    private static bool IsHiddenOrUnassigned(Rune character) =>
        BlankLookingCharacters.Contains(character.Value) || Rune.GetUnicodeCategory(character) switch
        {
            UnicodeCategory.Control or
            UnicodeCategory.LineSeparator or
            UnicodeCategory.ParagraphSeparator or
            UnicodeCategory.PrivateUse or
            UnicodeCategory.OtherNotAssigned => true,
            UnicodeCategory.Format => character.Value is not (ZeroWidthNonJoiner or ZeroWidthJoiner),
            _ => false
        };

    private static bool IsVisibleSign(Rune character) =>
        Rune.IsLetterOrDigit(character) || Rune.IsSymbol(character);

    private static bool HasStackedMarks(List<Rune> characters)
    {
        var marksInARow = 0;

        foreach (var character in characters)
        {
            marksInARow = IsCombiningMark(character) ? marksInARow + 1 : 0;

            if (marksInARow > MaximumCombiningMarksInARow)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCombiningMark(Rune character) =>
        Rune.GetUnicodeCategory(character) is UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.EnclosingMark;

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRun();
}

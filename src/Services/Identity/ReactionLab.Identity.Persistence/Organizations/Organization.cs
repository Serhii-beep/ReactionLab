using System.Security.Cryptography;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Persistence.Organizations;

public sealed class Organization
{
    public const int MaximumKindLength = 16;
    public const int MaximumNameLength = 128;
    public const int MaximumSignInCodeLength = 16;
    public const int MaximumTimeZoneLength = 64;
    public const string DefaultTimeZone = "Etc/UTC";

    public static readonly Error KindUnknown = Error.Validation(
        "Organization.KindUnknown",
        "An organization is a district or a school.",
        nameof(Kind));

    public static readonly Error DistrictNotAllowed = Error.Validation(
        "Organization.DistrictNotAllowed",
        "Only a school belongs to a district.",
        nameof(DistrictId));

    public static readonly Error NameRequired = Error.Validation(
        "Organization.NameRequired",
        "A name is required.",
        nameof(Name));

    public static readonly Error NameTooLong = Error.Validation(
        "Organization.NameTooLong",
        $"A name must not exceed {MaximumNameLength} characters.",
        nameof(Name))
        .WithArgs(("max", MaximumNameLength));

    public static readonly Error NameInvalid = Error.Validation(
        "Organization.NameInvalid",
        "A name needs a letter, digit or symbol, and no invisible, text-direction or stacked characters.",
        nameof(Name));

    public static readonly Error SignInCodeInvalid = Error.Validation(
        "Organization.SignInCodeInvalid",
        $"A sign-in code is 1 to {MaximumSignInCodeLength} letters and digits.",
        nameof(SignInCode))
        .WithArgs(("max", MaximumSignInCodeLength));

    public static readonly Error TimeZoneInvalid = Error.Validation(
        "Organization.TimeZoneInvalid",
        "A time zone is an IANA name, such as Europe/Kyiv.",
        nameof(TimeZone));

    private const int SignInCodeLength = 6;
    private const string SignInCodeCharacters = "23456789ABCDEFGHJKMNPQRSTVWXYZ";

    private Organization()
    {

    }

    public Guid Id { get; private set; }

    public string Kind { get; private set; } = string.Empty;

    public Guid? DistrictId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string SignInCode { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? SuspendedAt { get; private set; }

    public string Locale { get; private set; } = SupportedLocale.Default.Code;

    public string TimeZone { get; private set; } = DefaultTimeZone;

    public bool TeachersMayResetStudentPasswords { get; private set; }

    public static Result<Organization> Create(string? kind, Guid? districtId, string? name, DateTimeOffset now)
    {
        if (kind is not (OrganizationKinds.District or OrganizationKinds.School))
        {
            return KindUnknown;
        }

        if (kind == OrganizationKinds.District && districtId is not null)
        {
            return DistrictNotAllowed;
        }

        var normalizedName = NormalizeName(name);

        if (normalizedName.IsFailure)
        {
            return normalizedName.Error;
        }

        return new Organization
        {
            Id = Guid.CreateVersion7(now),
            Kind = kind,
            DistrictId = districtId,
            Name = normalizedName.Value,
            SignInCode = NewSignInCode(),
            CreatedAt = now
        };
    }

    public static Result<string> NormalizeName(string? name) =>
        DisplayNames.Normalize(name, MaximumNameLength, NameRequired, NameTooLong, NameInvalid);

    public static Result<string> NormalizeTimeZone(string? timeZone)
    {
        var trimmed = timeZone?.Trim() ?? string.Empty;

        return trimmed.Length is > 0 and <= MaximumTimeZoneLength
            && trimmed.Split('/').All(segment => segment.Length > 0)
            && TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out var zone)
            && zone.HasIanaId
            && zone.Id == trimmed
                ? trimmed
                : TimeZoneInvalid;
    }

    public static Result<string> NormalizeSignInCode(string? signInCode)
    {
        var trimmed = signInCode?.Trim() ?? string.Empty;

        return trimmed.Length is > 0 and <= MaximumSignInCodeLength && trimmed.All(char.IsAsciiLetterOrDigit)
            ? trimmed.ToUpperInvariant()
            : SignInCodeInvalid;
    }

    public void ReplaceSignInCode() => SignInCode = NewSignInCode();

    public Result ChangeSignInCode(string? signInCode)
    {
        var normalized = NormalizeSignInCode(signInCode);

        if (normalized.IsFailure)
        {
            return Result.Failure(normalized.Error);
        }

        SignInCode = normalized.Value;

        return Result.Success();
    }

    public Result<IReadOnlyList<string>> ChangeSettings(
        string? name,
        string? locale,
        string? timeZone,
        string? signInCode,
        bool? teachersMayResetStudentPasswords)
    {
        Result<string> newName = name is null ? Name : NormalizeName(name);
        Result<string> newLocale = locale is null ? Locale : LocaleCode(SupportedLocale.Create(locale));
        Result<string> newTimeZone = timeZone is null ? TimeZone : NormalizeTimeZone(timeZone);
        Result<string> newSignInCode = signInCode is null ? SignInCode : NormalizeSignInCode(signInCode);

        if (new[] { newName, newLocale, newTimeZone, newSignInCode }.FirstOrDefault(result => result.IsFailure)
            is { } failure)
        {
            return failure.Error;
        }

        var newTeacherResets = teachersMayResetStudentPasswords ?? TeachersMayResetStudentPasswords;
        string?[] changes =
        [
            newName.Value == Name ? null : OrganizationChangeNames.Renamed,
            newLocale.Value == Locale ? null : OrganizationChangeNames.LocaleChanged,
            newTimeZone.Value == TimeZone ? null : OrganizationChangeNames.TimeZoneChanged,
            newSignInCode.Value == SignInCode ? null : OrganizationChangeNames.SignInCodeChanged,
            newTeacherResets == TeachersMayResetStudentPasswords ? null
                : newTeacherResets ? OrganizationChangeNames.TeacherResetsAllowed
                : OrganizationChangeNames.TeacherResetsDisallowed
        ];

        Name = newName.Value;
        Locale = newLocale.Value;
        TimeZone = newTimeZone.Value;
        SignInCode = newSignInCode.Value;
        TeachersMayResetStudentPasswords = newTeacherResets;

        return changes.OfType<string>().ToList();
    }

    private static Result<string> LocaleCode(Result<SupportedLocale> locale) =>
        locale.IsSuccess ? locale.Value.Code : locale.Error;

    private static string NewSignInCode() =>
        RandomNumberGenerator.GetString(SignInCodeCharacters, SignInCodeLength);
}

using System.Security.Cryptography;
using ReactionLab.Identity.Persistence.Accounts;
using ReactionLab.SharedKernel;

namespace ReactionLab.Identity.Persistence.Organizations;

public sealed class Organization
{
    public const int MaximumKindLength = 16;
    public const int MaximumNameLength = 128;
    public const int MaximumSignInCodeLength = 16;

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

        var normalizedName = DisplayNames.Normalize(name, MaximumNameLength, NameRequired, NameTooLong, NameInvalid);

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

    public void ReplaceSignInCode() => SignInCode = NewSignInCode();

    private static string NewSignInCode() =>
        RandomNumberGenerator.GetString(SignInCodeCharacters, SignInCodeLength);
}

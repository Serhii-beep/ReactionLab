using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reference;

public sealed record ReferenceKey
{
    public const int MaximumLength = 50;

    public static readonly Error Required = Error.Validation(
        "ReferenceKey.Required",
        "A reference dataset must have a key.");

    public static readonly Error TooLong = Error.Validation(
        "ReferenceKey.TooLong",
        $"A reference key must not exceed {MaximumLength} characters.")
        .WithArgs(("max", MaximumLength));

    public static readonly Error Malformed = Error.Validation(
        "ReferenceKey.Malformed",
        "A reference key must be lowercase letters, digits, and single hyphens, beginning and ending with a letter or digit.");

    private ReferenceKey(string value) => Value = value;

    public string Value { get; }

    public static Result<ReferenceKey> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Required;
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaximumLength)
        {
            return TooLong;
        }

        return IsSlug(trimmed) ? new ReferenceKey(trimmed) : Malformed;
    }

    public override string ToString() => Value;

    private static bool IsSlug(string value)
    {
        if (!char.IsAsciiLetterOrDigit(value[0]) || !char.IsAsciiLetterOrDigit(value[^1]))
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '-')
            {
                if (value[i - 1] == '-')
                {
                    return false;
                }

                continue;
            }

            if (!char.IsAsciiDigit(value[i]) && !char.IsAsciiLetterLower(value[i]))
            {
                return false;
            }
        }

        return true;
    }
}

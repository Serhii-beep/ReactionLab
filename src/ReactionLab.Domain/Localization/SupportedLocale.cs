using System.Text.RegularExpressions;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Localization;

public sealed partial record SupportedLocale
{
    public static readonly SupportedLocale English = new("en");

    public static readonly SupportedLocale Ukrainian = new("uk");

    public static readonly Error Required = Error.Validation(
        "SupportedLocale.Required",
        "A locale code is required.");

    public static readonly Error Malformed = Error.Validation(
        "SupportedLocale.Malformed",
        "A locale code must be a BCP 47 tag.");

    public static readonly Error Unsupported = Error.Validation(
        "SupportedLocale.Unsupported",
        "The provided locale is not supported.");

    private SupportedLocale(string code) => Code = code;

    public static SupportedLocale Default => English;

    public static IReadOnlyList<SupportedLocale> All { get; } = [English, Ukrainian];

    public string Code { get; }

    public static Result<SupportedLocale> Create(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Required;
        }

        var trimmed = code.Trim();

        if (!TagPattern().IsMatch(trimmed))
        {
            return Malformed;
        }

        var normalized = Normalize(trimmed);

        return All.FirstOrDefault(locale => locale.Code == normalized) is { } supported
            ? supported
            : Unsupported;
    }

    public static SupportedLocale OrDefault(string? code) =>
        Create(code) is { IsSuccess: true } result ? result.Value : Default;

    public override string ToString() => Code;

    private static string Normalize(string tag)
    {
        var parts = tag.Split('-');

        return parts.Length == 1
            ? parts[0].ToLowerInvariant()
            : $"{parts[0].ToLowerInvariant()}-{parts[1].ToUpperInvariant()}";
    }

    [GeneratedRegex(@"^[A-Za-z]{2,3}(-[A-Za-z]{2})?$", RegexOptions.CultureInvariant)]
    private static partial Regex TagPattern();
}

using System.Text.RegularExpressions;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.SharedKernel;

public sealed partial record HexColor
{
    public static readonly Error Invalid = Error.Validation(
        "HexColor.Invalid",
        "Color must be a six-digit hexadecimal RGB value, for example #FFFFFF.");

    private HexColor(string value) => Value = value;

    public string Value { get; }

    public static Result<HexColor> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Invalid;
        }

        var trimmed = value.Trim();

        return HexPattern().IsMatch(trimmed)
            ? new HexColor(trimmed.ToUpperInvariant())
            : Invalid;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexPattern();
}

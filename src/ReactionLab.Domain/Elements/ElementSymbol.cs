using System.Text.RegularExpressions;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Elements;

public sealed partial record ElementSymbol
{
    public static readonly Error Invalid = Error.Validation(
        "ElementSymbol.Invalid",
        "Symbol must be an uppercase letter followed by up to two lowercase letters.");

    private ElementSymbol(string value) => Value = value;

    public string Value { get; }

    public static Result<ElementSymbol> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Invalid;
        }

        var trimmed = value.Trim();

        return SymbolPattern().IsMatch(trimmed) ? new ElementSymbol(trimmed) : Invalid;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z][a-z]{0,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex SymbolPattern();
}

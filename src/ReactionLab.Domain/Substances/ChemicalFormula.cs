using System.Text;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Domain.Substances;

public sealed record ChemicalFormula
{
    public const int MaximumLength = 100;

    public static readonly Error Empty = Error.Validation(
        "ChemicalFormula.Empty",
        "A chemical formula is required.");

    public static readonly Error TooLong = Error.Validation(
        "ChemicalFormula.TooLong",
        $"A chemical formula must not exceed {MaximumLength} characters.")
        .WithArgs(("max", MaximumLength));

    public static readonly Error Malformed = Error.Validation(
        "ChemicalFormula.Malformed",
        "The formula contains a character that is not part of an element symbol, a count, or a group.");

    public static readonly Error UnbalancedParentheses = Error.Validation(
        "ChemicalFormula.UnbalancedParentheses",
        "The formula has unbalanced parentheses.");

    public static readonly Error InvalidCount = Error.Validation(
        "ChemicalFormula.InvalidCount",
        "A subscript must be a positive integer without leading zero.");

    private ChemicalFormula(string value, string hill)
    {
        Value = value;
        Hill = hill;
    }

    public string Value { get; }

    public string Hill { get; }

    public IReadOnlyList<ElementQuantity> Composition => ToHillOrder(Tally(Value).Value);

    public static Result<ChemicalFormula> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Empty;
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaximumLength)
        {
            return TooLong;
        }

        var tally = Tally(trimmed);
        if (tally.IsFailure)
        {
            return tally.Error;
        }

        if (tally.Value.Count == 0)
        {
            return Empty;
        }

        return new ChemicalFormula(trimmed, BuildHill(ToHillOrder(tally.Value)));
    }

    public int CountOf(ElementSymbol symbol) =>
        Composition.FirstOrDefault(q => q.Symbol == symbol).Count;

    public override string ToString() => Value;

    private static Result<Dictionary<ElementSymbol, int>> Tally(string formula)
    {
        var index = 0;
        var parsed = ParseSequence(formula, ref index);

        if (parsed.IsFailure)
        {
            return parsed.Error;
        }

        return index == formula.Length ? parsed : UnbalancedParentheses;
    }

    private static Result<Dictionary<ElementSymbol, int>> ParseSequence(string formula, ref int index)
    {
        var tally = new Dictionary<ElementSymbol, int>();

        while (index < formula.Length && formula[index] != ')')
        {
            if (formula[index] == '(')
            {
                index++;

                var group = ParseSequence(formula, ref index);
                if (group.IsFailure)
                {
                    return group.Error;
                }

                if (index >= formula.Length || formula[index] != ')')
                {
                    return UnbalancedParentheses;
                }

                index++;

                var groupMultiplier = ParseCount(formula, ref index);
                if (groupMultiplier.IsFailure)
                {
                    return groupMultiplier.Error;
                }

                foreach (var (symbol, count) in group.Value)
                {
                    Add(tally, symbol, count * groupMultiplier.Value);
                }

                continue;
            }

            if (!char.IsAsciiLetterUpper(formula[index]))
            {
                return Malformed;
            }

            var symbolText = ReadSymbol(formula, ref index);
            var elementSymbol = ElementSymbol.Create(symbolText);
            if (elementSymbol.IsFailure)
            {
                return Malformed;
            }

            var atomCount = ParseCount(formula, ref index);
            if (atomCount.IsFailure)
            {
                return atomCount.Error;
            }

            Add(tally, elementSymbol.Value, atomCount.Value);
        }

        return tally;
    }

    private static string ReadSymbol(string formula, ref int index)
    {
        var start = index;
        index++;

        while (index < formula.Length && index - start < 3 && char.IsAsciiLetterLower(formula[index]))
        {
            index++;
        }

        return formula[start..index];
    }

    private static Result<int> ParseCount(string formula, ref int index)
    {
        if (index >= formula.Length || !char.IsAsciiDigit(formula[index]))
        {
            return 1;
        }

        if (formula[index] == '0')
        {
            return InvalidCount;
        }

        var start = index;
        while (index < formula.Length && char.IsAsciiDigit(formula[index]))
        {
            index++;
        }

        return int.TryParse(formula[start..index], out var count) ? count : InvalidCount;
    }

    private static void Add(Dictionary<ElementSymbol, int> tally, ElementSymbol symbol, int count) =>
        tally[symbol] = tally.TryGetValue(symbol, out var existing) ? existing + count : count;

    private static List<ElementQuantity> ToHillOrder(Dictionary<ElementSymbol, int> tally)
    {
        var ordered = new List<ElementQuantity>();
        var remaining = new Dictionary<ElementSymbol, int>(tally);

        if (remaining.Keys.FirstOrDefault(s => s.Value == "C") is { } carbon)
        {
            ordered.Add(new ElementQuantity(carbon, remaining[carbon]));
            remaining.Remove(carbon);

            if (remaining.Keys.FirstOrDefault(s => s.Value == "H") is { } hydrogen)
            {
                ordered.Add(new ElementQuantity(hydrogen, remaining[hydrogen]));
                remaining.Remove(hydrogen);
            }
        }

        ordered.AddRange(remaining
            .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
            .Select(pair => new ElementQuantity(pair.Key, pair.Value)));

        return ordered;
    }

    private static string BuildHill(IReadOnlyList<ElementQuantity> composition)
    {
        var sb = new StringBuilder();

        foreach (var (symbol, count) in composition)
        {
            sb.Append(symbol.Value);
            if (count > 1)
            {
                sb.Append(count);
            }
        }

        return sb.ToString();
    }
}

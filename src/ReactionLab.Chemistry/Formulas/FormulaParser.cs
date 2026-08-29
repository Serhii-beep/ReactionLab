using System.Globalization;

namespace ReactionLab.Chemistry.Formulas;

public static class FormulaParser
{
    public const int MaximumLength = 100;

    public static bool TryParse(
        string? formula,
        out ChemicalComposition composition,
        out FormulaError error)
    {
        composition = null!;

        if (string.IsNullOrWhiteSpace(formula))
        {
            error = FormulaError.Empty;
            return false;
        }

        var text = formula.Trim();

        if (text.Length > MaximumLength)
        {
            error = FormulaError.TooLong;
            return false;
        }

        var charge = 0;
        var caret = text.IndexOf('^', StringComparison.Ordinal);

        if (caret >= 0)
        {
            if (!TryParseCharge(text[(caret + 1)..], out charge))
            {
                error = FormulaError.InvalidCharge;
                return false;
            }

            text = text[..caret];

            if (text.Length == 0)
            {
                error = FormulaError.Empty;
                return false;
            }
        }

        var index = 0;
        var tally = new Dictionary<string, int>(StringComparer.Ordinal);

        if (!ParseSequence(text, ref index, tally, out error))
        {
            return false;
        }

        if (index != text.Length)
        {
            error = FormulaError.UnbalancedGroup;
            return false;
        }

        if (tally.Count == 0)
        {
            error = FormulaError.Empty;
            return false;
        }

        composition = new ChemicalComposition(tally, charge);
        error = FormulaError.None;
        return true;
    }

    private static bool TryParseCharge(string suffix, out int charge)
    {
        charge = 0;

        if (suffix.Length == 0)
        {
            return false;
        }

        var sign = suffix[^1];

        if (sign is not ('+' or '-'))
        {
            return false;
        }

        var magnitudeText = suffix[..^1];
        var magnitude = 1;

        if (magnitudeText.Length > 0
            && (magnitudeText[0] == '0'
                || !int.TryParse(magnitudeText, NumberStyles.None, CultureInfo.InvariantCulture, out magnitude)
                || magnitude < 1))
        {
            return false;
        }

        charge = sign == '+' ? magnitude : -magnitude;
        return true;
    }

    private static bool ParseSequence(
        string text,
        ref int index,
        Dictionary<string, int> tally,
        out FormulaError error)
    {
        error = FormulaError.None;

        while (index < text.Length && text[index] is not (')' or ']'))
        {
            if (text[index] is '(' or '[')
            {
                var close = text[index] == '(' ? ')' : ']';
                index++;

                var group = new Dictionary<string, int>(StringComparer.Ordinal);

                if (!ParseSequence(text, ref index, group, out error))
                {
                    return false;
                }

                if (index >= text.Length || text[index] != close)
                {
                    error = FormulaError.UnbalancedGroup;
                    return false;
                }

                index++;

                if (group.Count == 0)
                {
                    error = FormulaError.Malformed;
                    return false;
                }

                if (!TryParseCount(text, ref index, out var multiplier, out error))
                {
                    return false;
                }

                foreach (var (groupSymbol, count) in group)
                {
                    Add(tally, groupSymbol, count * multiplier);
                }

                continue;
            }

            if (!char.IsAsciiLetterUpper(text[index]))
            {
                error = FormulaError.Malformed;
                return false;
            }

            var start = index++;

            while (index < text.Length && index - start < 3 && char.IsAsciiLetterLower(text[index]))
            {
                index++;
            }

            var symbol = text[start..index];

            if (!TryParseCount(text, ref index, out var atoms, out error))
            {
                return false;
            }

            Add(tally, symbol, atoms);
        }

        return true;
    }

    private static bool TryParseCount(string text, ref int index, out int count, out FormulaError error)
    {
        error = FormulaError.None;
        count = 1;

        if (index >= text.Length || !char.IsAsciiDigit(text[index]))
        {
            return true;
        }

        if (text[index] == '0')
        {
            error = FormulaError.InvalidCount;
            return false;
        }

        var start = index++;

        while (index < text.Length && char.IsAsciiDigit(text[index]))
        {
            index++;
        }

        if (!int.TryParse(text[start..index], out count))
        {
            error = FormulaError.InvalidCount;
            return false;
        }

        return true;
    }

    private static void Add(Dictionary<string, int> tally, string symbol, int count) =>
        tally[symbol] = tally.TryGetValue(symbol, out var existing) ? existing + count : count;
}

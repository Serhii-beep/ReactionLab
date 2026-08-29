using System.Text;
using ReactionLab.Chemistry.Formulas;
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

    public static readonly Error InvalidCharge = Error.Validation(
        "ChemicalFormula.InvalidCharge",
        "A charge must be a sign, optionally preceded by a magnitude.");

    private ChemicalFormula(string value, ChemicalComposition parsed)
    {
        Value = value;
        Hill = parsed.Hill;
        Charge = parsed.Charge;

        Composition =
        [
            .. parsed.Elements.Select(element =>
                new ElementQuantity(ElementSymbol.Create(element.Symbol).Value, element.Count))
        ];
    }

    public string Value { get; }

    public string Hill { get; }

    public int Charge { get; }

    public IReadOnlyList<ElementQuantity> Composition { get; }

    public static Result<ChemicalFormula> Create(string? value)
    {
        if (!FormulaParser.TryParse(value, out var parsed, out var error))
        {
            return Translate(error);
        }

        return new ChemicalFormula(value!.Trim(), parsed);
    }

    public int CountOf(ElementSymbol symbol) =>
        Composition.FirstOrDefault(q => q.Symbol == symbol).Count;

    public override string ToString() => Value;

    public bool Equals(ChemicalFormula? other) => other is not null && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    private static Error Translate(FormulaError error) => error switch
    {
        FormulaError.Empty => Empty,
        FormulaError.TooLong => TooLong,
        FormulaError.UnbalancedGroup => UnbalancedParentheses,
        FormulaError.InvalidCount => InvalidCount,
        FormulaError.InvalidCharge => InvalidCharge,
        _ => Malformed
    };
}

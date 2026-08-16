using System.Globalization;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Substances;

public sealed record MolecularWeight
{
    public const decimal Maximum = 100000m;

    public static readonly Error NotPositive = Error.Validation(
        "MolecularWeight.NotPositive",
        "Molecular weight must be greater than zero.");

    public static readonly Error Implausible = Error.Validation(
        "MolecularWeight.Implausible",
        $"Molecular weight above {Maximum} g/mol usually indicates a unit error.");

    private MolecularWeight(decimal gramsPerMole) => GramsPerMole = gramsPerMole;

    public decimal GramsPerMole { get; }

    public static Result<MolecularWeight> Create(decimal gramsPerMole) => gramsPerMole switch
    {
        <= 0 => NotPositive,
        > Maximum => Implausible,
        _ => new MolecularWeight(gramsPerMole)
    };

    public override string ToString() => $"{GramsPerMole.ToString("0.####", CultureInfo.InvariantCulture)} g/mol";
}

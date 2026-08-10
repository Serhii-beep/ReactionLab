using System.Globalization;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Elements;

public sealed record AtomicMass
{
    public const decimal Maximum = 500m;

    public static readonly Error NotPositive = Error.Validation(
        "AtomicMass.NotPositive",
        "Atomic mass must be greater than zero.");

    public static readonly Error Implausible = Error.Validation(
        "AtomicMass.Implausible",
        $"Atomic mass above {Maximum} u exceeds any known element.");

    private AtomicMass(decimal daltons) => Daltons = daltons;

    public decimal Daltons { get; }

    public static Result<AtomicMass> Create(decimal daltons) => daltons switch
    {
        <= 0 => NotPositive,
        > Maximum => Implausible,
        _ => new AtomicMass(daltons)
    };

    public override string ToString() => $"{Daltons.ToString("0.###", CultureInfo.InvariantCulture)} u";
}

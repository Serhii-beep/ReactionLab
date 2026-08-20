using System.Globalization;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Elements;

public sealed record Electronegativity
{
    public const decimal Minimum = 0.5m;
    public const decimal Maximum = 4.0m;

    public static readonly Error OutOfRange = Error.Validation(
        "Electronegativity.OutOfRange",
        $"Pauling electronegativity must be between {Minimum} and {Maximum}.")
        .WithArgs(("min", Minimum), ("max", Maximum));

    private Electronegativity(decimal pauling) => Pauling = pauling;

    public decimal Pauling { get; }

    public static Result<Electronegativity> Create(decimal pauling) =>
        pauling is < Minimum or > Maximum ? OutOfRange : new Electronegativity(pauling);

    public override string ToString() => Pauling.ToString("0.##", CultureInfo.InvariantCulture);
}

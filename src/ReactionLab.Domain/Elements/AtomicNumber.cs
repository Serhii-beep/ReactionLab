using System.Globalization;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Elements;

public sealed record AtomicNumber
{
    public const int Minimum = 1;
    public const int Maximum = 118;

    public static readonly Error OutOfRange = Error.Validation(
        "AtomicNumber.OutOfRange",
        $"Atomic number must be between {Minimum} and {Maximum}.");

    private AtomicNumber(int value) => Value = value;

    public int Value { get; }

    public static Result<AtomicNumber> Create(int value) =>
        value is < Minimum or > Maximum ? OutOfRange : new AtomicNumber(value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

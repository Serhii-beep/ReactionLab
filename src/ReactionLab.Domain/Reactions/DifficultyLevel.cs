using System.Globalization;
using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reactions;

public sealed record DifficultyLevel
{
    public const int Minimum = 1;
    public const int Maximum = 5;

    public static readonly Error OutOfRange = Error.Validation(
        "DifficultyLevel.OutOfRange",
        $"Difficulty must be between {Minimum} and {Maximum}.");

    private DifficultyLevel(int value) => Value = value;

    public int Value { get; }

    public static DifficultyLevel Introductory { get; } = new(Minimum);

    public static Result<DifficultyLevel> Create(int value) =>
        value is < Minimum or > Maximum ? OutOfRange : new DifficultyLevel(value);

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}

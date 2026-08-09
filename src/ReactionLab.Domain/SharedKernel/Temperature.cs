using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.SharedKernel;

public sealed record Temperature
{
    public const decimal KelvinAtZeroCelsius = 273.15m;

    public static readonly Error BelowAbsoluteZero = Error.Validation(
        "Temperature.BelowAbsoluteZero",
        "Temperature cannot be below absolute zero (0K).");

    public static readonly Temperature RoomTemperature = new(298.15m);

    private Temperature(decimal kelvin) => Kelvin = kelvin;

    public decimal Kelvin { get; }

    public decimal Celsius => Kelvin - KelvinAtZeroCelsius;

    public static Result<Temperature> FromKelvin(decimal kelvin) =>
        kelvin < 0 ? BelowAbsoluteZero : new Temperature(kelvin);

    public static Result<Temperature> FromCelsius(decimal celsius) =>
        FromKelvin(celsius + KelvinAtZeroCelsius);

    public override string ToString() => $"{Kelvin:0.##} K";
}

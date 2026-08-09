using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.SharedKernel;

public sealed record Pressure
{
    public const decimal StandardAtmosphereInKilopascals = 101.325m;

    public static readonly Error NotPositive = Error.Validation(
        "Pressure.NotPositive",
        "Pressure must be greater than zero.");

    public static readonly Pressure StandardAtmosphere = new(StandardAtmosphereInKilopascals);

    private Pressure(decimal kilopascals) => Kilopascals = kilopascals;

    public decimal Kilopascals { get; }

    public decimal Atmospheres => Kilopascals / StandardAtmosphereInKilopascals;

    public static Result<Pressure> FromKilopascals(decimal kilopascals) =>
        kilopascals <= 0 ? NotPositive : new Pressure(kilopascals);

    public static Result<Pressure> FromAtmospheres(decimal atmospheres) =>
        FromKilopascals(atmospheres * StandardAtmosphereInKilopascals);

    public override string ToString() => $"{Kilopascals:0.##} kPa";
}

using ReactionLab.Domain.Common;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Domain.Reactions;

public sealed record ReactionConditions
{
    public const int MaximumCatalystLength = 200;

    public static readonly Error CatalystTooLong = Error.Validation(
        "ReactionConditions.CatalystTooLong",
        $"Catalyst description must not exceed {MaximumCatalystLength} characters.");

    private ReactionConditions(Temperature? temperature, Pressure? pressure, string? catalyst)
    {
        Temperature = temperature;
        Pressure = pressure;
        Catalyst = catalyst;
    }

    public Temperature? Temperature { get; }

    public Pressure? Pressure { get; }

    public string? Catalyst { get; }

    public bool RequiredCatalyst => Catalyst is not null;

    public static ReactionConditions Ambient { get; } = new(Temperature.RoomTemperature, Pressure.StandardAtmosphere, null);

    public static ReactionConditions Unspecified { get; } = new(null, null, null);

    public static Result<ReactionConditions> Create(
        Temperature? temperature,
        Pressure? pressure,
        string? catalyst)
    {
        if (string.IsNullOrWhiteSpace(catalyst))
        {
            return new ReactionConditions(temperature, pressure, null);
        }

        var trimmed = catalyst.Trim();

        return trimmed.Length > MaximumCatalystLength
            ? CatalystTooLong
            : new ReactionConditions(temperature, pressure, trimmed);
    }
}

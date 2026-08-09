using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.SharedKernel;

public sealed record Enthalpy
{
    public const decimal ImplausibleMagnitude = 100000m;

    public static readonly Error Implausible = Error.Validation(
        "Enthalpy.Implausible",
        $"Enthalpy magnitude exceeds {ImplausibleMagnitude} kJ/mol, which ususally means a unit error.");

    private Enthalpy(decimal kilojoulesPerMole) => KilojoulesPerMole = kilojoulesPerMole;

    public decimal KilojoulesPerMole { get; }

    public bool IsExothermic => KilojoulesPerMole < 0;

    public bool IsEndothermic => KilojoulesPerMole > 0;

    public bool IsThermoneutral => KilojoulesPerMole == 0;

    public static Result<Enthalpy> FromKilojoulesPerMole(decimal kilojoulesPerMole) =>
        Math.Abs(kilojoulesPerMole) > ImplausibleMagnitude ? Implausible : new Enthalpy(kilojoulesPerMole);

    public override string ToString() => $"{KilojoulesPerMole:+0.##;-0.##;0} kJ/mol";
}

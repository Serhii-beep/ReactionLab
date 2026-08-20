using ReactionLab.Domain.Common;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Domain.Reactions;

public sealed record Thermodynamics
{
    public const decimal MaximumActivationEnergy = 100000;

    public static readonly Error NegativeActivationEnergy = Error.Validation(
        "Thermodynamics.NegativeActivationEnergy",
        "Activation energy cannot be negative.");

    public static readonly Error ImplausibleActivationEnergy = Error.Validation(
        "Thermodynamics.ImplausibleActivationEnergy",
        $"Activation energy above {MaximumActivationEnergy} kJ/mol usually indicates a unit error.")
        .WithArgs(("max", MaximumActivationEnergy));

    public static readonly Error ActivationEnergyBelowEnthalpy = Error.Validation(
        "Thermodynamics.ActivationEnergyBelowEnthalpy",
        "Activation energy cannot be lower than the enthalpy change of an endothermic reaction.");

    private Thermodynamics(Enthalpy? enthalpyChange, decimal? activationEnergyKilojoulesPerMole)
    {
        EnthalpyChange = enthalpyChange;
        ActivationEnergyKilojoulesPerMole = activationEnergyKilojoulesPerMole;
    }

    public Enthalpy? EnthalpyChange { get; }

    public decimal? ActivationEnergyKilojoulesPerMole { get; }

    public bool? IsExothermic => EnthalpyChange?.IsExothermic;

    public bool? IsEndothermic => EnthalpyChange?.IsEndothermic;

    public decimal? ReverseActivationEnergyKilojoulesPerMole =>
        ActivationEnergyKilojoulesPerMole is { } forward && EnthalpyChange is { } enthalpy
            ? forward - enthalpy.KilojoulesPerMole
            : null;

    public static Thermodynamics Unknown { get; } = new(null, null);

    public static Result<Thermodynamics> Create(Enthalpy? enthalpyChange, decimal? activationEnergy)
    {
        if (activationEnergy is { } energy)
        {
            if (energy < 0)
            {
                return NegativeActivationEnergy;
            }

            if (energy > MaximumActivationEnergy)
            {
                return ImplausibleActivationEnergy;
            }

            if (enthalpyChange is { } enthalpy
                && enthalpy.KilojoulesPerMole > 0
                && energy < enthalpy.KilojoulesPerMole)
            {
                return ActivationEnergyBelowEnthalpy;
            }
        }

        return new Thermodynamics(enthalpyChange, activationEnergy);
    }
}

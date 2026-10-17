using ReactionLab.Chemistry.Ions;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.UnitTests.Ions;

internal static class TestIons
{
    public static IonTable Table { get; } = new(
        [
            new("Li", 1), new("Na", 1), new("K", 1), new("Ag", 1), new("NH4", 1),
            new("Mg", 2), new("Ca", 2), new("Ba", 2), new("Zn", 2), new("Cu", 2),
            new("Fe", 2), new("Pb", 2), new("Al", 3), new("Fe", 3),
        ],
        [
            new("F", -1), new("Cl", -1), new("Br", -1), new("I", -1), new("OH", -1),
            new("NO3", -1), new("MnO4", -1), new("HCO3", -1), new("CH3COO", -1),
            new("O", -2), new("S", -2), new("SO4", -2), new("CO3", -2),
            new("N", -3), new("PO4", -3), new("NO2", -1)
        ],
        [
            new("groupOneAndAmmonium", Solubility.Soluble, Cations: ["Li", "Na", "K", "Rb", "Cs", "NH4"]),
            new("nitratesAndAcetates", Solubility.Soluble, Anions: ["NO3", "NO2", "ClO3", "ClO", "CH3COO"]),
            new("halides", Solubility.Soluble, Anions: ["Cl", "Br", "I"], ExceptCations: ["Ag", "Pb"]),
            new("sulfates", Solubility.Soluble, Anions: ["SO4"], ExceptCations: ["Ba", "Pb", "Ca", "Sr", "Ag"]),
            new("hydroxides", Solubility.Insoluble, Anions: ["OH"], ExceptCations: ["Ba", "Sr"]),
            new("carbonatesAndTheRest", Solubility.Insoluble),
        ],
        new IonBehaviors(
            ["Li", "Na", "K", "Rb", "Cs", "NH4"],
            ["NO3", "NO2"],
            ["CO3"],
            ["N", "O"]));

    public static ActivitySeries Series { get; } = new(
    [
        new("K", 1, WaterReactivity.Cold),
        new("Na", 1, WaterReactivity.Cold),
        new("Li", 1, WaterReactivity.Cold),
        new("Ba", 2, WaterReactivity.Cold),
        new("Ca", 2, WaterReactivity.Cold),
        new("Mg", 2, WaterReactivity.Steam),
        new("Al", 3, WaterReactivity.Steam),
        new("Zn", 2, WaterReactivity.Steam),
        new("Fe", 2, WaterReactivity.Steam),
        new("Ni", 2, WaterReactivity.None),
        new("Sn", 2, WaterReactivity.None),
        new("Pb", 2, WaterReactivity.None),
        new("H", 1, WaterReactivity.None),
        new("Cu", 2, WaterReactivity.None),
        new("Ag", 1, WaterReactivity.None),
        new("Au", 3, WaterReactivity.None),
    ]);

    public static IReadOnlyDictionary<string, Phase> StandardStates { get; } = new Dictionary<string, Phase>(StringComparer.Ordinal)
    {
        ["H2"] = Phase.Gas,
        ["O2"] = Phase.Gas,
        ["N2"] = Phase.Gas,
        ["Cl2"] = Phase.Gas,
        ["CO2"] = Phase.Gas,
        ["CO"] = Phase.Gas,
        ["SO2"] = Phase.Gas,
        ["H2O"] = Phase.Liquid,
        ["HCl"] = Phase.Liquid,
        ["CH4"] = Phase.Gas,
        ["C2H6O"] = Phase.Liquid,
        ["C"] = Phase.Solid,
        ["S"] = Phase.Solid,
        ["Na"] = Phase.Solid,
        ["K"] = Phase.Solid,
        ["Mg"] = Phase.Solid,
        ["Ca"] = Phase.Solid,
        ["Zn"] = Phase.Solid,
        ["Fe"] = Phase.Solid,
        ["Cu"] = Phase.Solid,
        ["Ag"] = Phase.Solid,
        ["Al"] = Phase.Solid,
        ["MgO"] = Phase.Solid,
        ["CaO"] = Phase.Solid,
        ["CaCO3"] = Phase.Solid,
        ["Na2CO3"] = Phase.Solid,
        ["NaHCO3"] = Phase.Solid,
        ["Ca(OH)2"] = Phase.Solid
    };
}

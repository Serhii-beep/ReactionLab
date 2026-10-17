using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser.Contracts;

internal sealed record EquilibriumEntry(
    decimal EnthalpyKilojoulesPerMole,
    decimal EntropyJoulesPerMoleKelvin,
    decimal GibbsKilojoulesPerMole,
    double Log10EquilibriumConstant,
    decimal? CrossoverKelvin,
    IReadOnlyList<Phase> Phases,
    double Conversion,
    IReadOnlyList<double> MolesAtEquilibrium,
    decimal? HalfConversionKelvin);

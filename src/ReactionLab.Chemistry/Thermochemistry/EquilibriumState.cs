namespace ReactionLab.Chemistry.Thermochemistry;

public sealed record EquilibriumState(
    decimal EnthalpyKjPerMol,
    decimal EntropyJPerMolKelvin,
    decimal GibbsKjPerMol,
    double Log10EquilibriumConstant,
    IReadOnlyList<Phase> Phases,
    double Extent,
    double MinimumExtent,
    double MaximumExtent,
    IReadOnlyList<double> MolesAtEquilibrium)
{
    public double Conversion => MaximumExtent > MinimumExtent ? (Extent - MinimumExtent) / (MaximumExtent - MinimumExtent) : 0d;
}

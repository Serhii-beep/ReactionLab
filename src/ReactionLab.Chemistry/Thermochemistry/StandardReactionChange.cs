namespace ReactionLab.Chemistry.Thermochemistry;

public sealed record StandardReactionChange(decimal EnthalpyKjPerMol, decimal EntropyJPerMolKelvin, IReadOnlyList<Phase> Phases)
{
    public decimal GibbsKjPerMolAt(decimal kelvin) => ReactionEnergetics.GibbsFreeEnergy(EnthalpyKjPerMol, EntropyJPerMolKelvin, kelvin);
}

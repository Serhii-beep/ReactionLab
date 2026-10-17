using ReactionLab.Chemistry.Browser.Contracts;
using ReactionLab.Chemistry.Geometry;
using ReactionLab.Chemistry.Ions;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Prediction.Rules;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser;

internal sealed record ChemistryReference(
    IReadOnlyDictionary<string, decimal> AtomicMasses,
    IReadOnlyDictionary<string, int> Periods,
    IReadOnlyDictionary<string, AtomicGeometry> AtomicGeometry,
    PredictionModel Prediction)
{
    public static ChemistryReference From(IReadOnlyList<ElementEntry> elements, ChemistryReferenceDocument document)
    {
        var ions = IonTableOf(document.Ions);
        var series = new ActivitySeries(
            [.. document.ActivitySeries.Metals.Select(metal => new ActiveMetal(metal.Symbol, metal.Charge, metal.Water))]);

        return new ChemistryReference(
            elements.ToDictionary(element => element.Symbol, element => element.Mass, StringComparer.Ordinal),
            elements.ToDictionary(element => element.Symbol, element => element.Period, StringComparer.Ordinal),
            document.AtomicGeometry.Elements.ToDictionary(
                entry => entry.Symbol,
                entry => new AtomicGeometry(
                    entry.ValenceElectrons,
                    entry.SingleBondRadiusPicometers,
                    entry.DoubleBondRadiusPicometers,
                    entry.TripleBondRadiusPicometers,
                    entry.AromaticBondRadiusPicometers),
                StringComparer.Ordinal),
            new PredictionModel(
                ions,
                new ReactionPredictor(
                [
                    new CombustionRule(),
                    new NeutralizationRule(ions),
                    new PrecipitationRule(ions),
                    new SingleReplacementRule(series, ions),
                    new SynthesisRule(series, ions),
                    new DecompositionRule(ions)
                ]),
                EnergyEstimatorOf(document.Thermodynamics, ions)));
    }

    private static IonTable IonTableOf(IonDataset dataset) =>
        new(
            [.. dataset.Cations.Select(ion => new Ion(ion.Formula, ion.Charge))],
            [.. dataset.Anions.Select(ion => new Ion(ion.Formula, ion.Charge))],
            [.. dataset.Solubility.Select(rule =>
                new SolubilityRule(rule.Code, rule.Solubility, rule.Cations, rule.Anions, rule.ExceptCations))],
            new IonBehaviors(
                dataset.Behaviors.ThermallyStableCations,
                dataset.Behaviors.OxidizingAnions,
                dataset.Behaviors.UnstableAcidAnions,
                dataset.Behaviors.HydrolyzingAnions));

    private static EnergyEstimator EnergyEstimatorOf(ThermodynamicsDataset dataset, IonTable ions) =>
        new(
            new StandardStateTable(
                [.. dataset.Species.Select(species =>
                    new SpeciesEnthalpy(species.Formula, species.Phase, species.FormationEnthalpyKilojoulesPerMole))],
                [.. dataset.AqueousIons.Select(ion =>
                    new AqueousIon(new Ion(ion.Formula, ion.Charge), ion.FormationEnthalpyKilojoulesPerMole))],
                ions),
            [.. dataset.Barriers.Select(barrier => new ActivationBarrier(
                barrier.Family,
                barrier.IntrinsicKilojoulesPerMole,
                barrier.TransferCoefficient,
                barrier.MinimumKilojoulesPerMole))]);
}

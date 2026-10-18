using ReactionLab.Chemistry.Browser.Contracts;
using ReactionLab.Chemistry.Generation;
using ReactionLab.Chemistry.Ions;
using ReactionLab.Chemistry.Prediction;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser;

internal sealed record PredictionModel(
    IonTable Ions,
    ReactionPredictor Predictor,
    EnergyEstimator Energetics,
    IReadOnlyDictionary<string, Phase> RoomPhases)
{
    private const string InvalidFormula = "InvalidFormula";

    public PredictionResult Predict(PredictionRequest request)
    {
        var reagents = new List<Reagent>();

        foreach (var formula in request.Formulas.Distinct(StringComparer.Ordinal))
        {
            if (!Reagent.TryCreate(formula, out var reagent))
            {
                return new PredictionResult(null, InvalidFormula, formula);
            }

            reagents.Add(reagent);
        }

        var standardStates = new Dictionary<string, Phase>(RoomPhases, StringComparer.Ordinal);

        foreach (var (formula, phase) in request.StandardStates ?? new Dictionary<string, Phase>())
        {
            standardStates[formula] = phase;
        }

        var phases = new PhaseResolution(Ions, standardStates);

        return new PredictionResult(
            [.. new ReactionGenerator(Predictor, phases, Energetics).From(reagents).Select(Entry)], null, null);
    }

    private static PredictedReactionEntry Entry(GeneratedReaction reaction) =>
        new(
            [.. reaction.Reactants.Select(Participant)],
            [.. reaction.Products.Select(Participant)],
            reaction.Rule,
            reaction.Confidence,
            reaction.MinimumKelvin,
            reaction.EnthalpyKjPerMol,
            reaction.ActivationEnergyKjPerMol);

    private static PredictedParticipantEntry Participant(GeneratedParticipant participant) =>
        new(participant.Formula, participant.Coefficient, participant.Phase);
}

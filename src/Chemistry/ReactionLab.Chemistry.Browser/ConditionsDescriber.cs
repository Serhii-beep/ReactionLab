using ReactionLab.Chemistry.Browser.Contracts;
using ReactionLab.Chemistry.Kinetics;
using ReactionLab.Chemistry.Thermochemistry;

namespace ReactionLab.Chemistry.Browser;

internal static class ConditionsDescriber
{
    private const decimal ReferenceKelvin = 298.15m;
    private const decimal ReferenceKilopascals = 101.325m;
    private const decimal CurveFirstKelvin = 273.15m;
    private const decimal CurveStepKelvin = 25m;
    private const int CurvePoints = 61;
    private const decimal CurvePressureMultiplier = 10m;
    private const double HalfConversion = 0.5d;
    private const int CrossingBisections = 24;

    private static readonly decimal[] CurveKelvins =
        [.. Enumerable.Range(0, CurvePoints).Select(point => CurveFirstKelvin + (point * CurveStepKelvin))];

    public static ConditionsResult Describe(ConditionsRequest request, StandardStateTable table) =>
        new(
            Arrhenius.ThermalEnergyKjPerMol(request.Kelvin),
            [.. request.Reactions.Select(reaction => DescribeReaction(reaction, request, table))]);

    private static ReactionConditionsEntry DescribeReaction(ConditionsReactionEntry reaction, ConditionsRequest request, StandardStateTable table)
    {
        List<string> speciesWithoutEntropy =
        [
            .. reaction.Participants
                .Where(participant => !table.HasEntropy(participant.Formula, participant.Phase))
                .Select(participant => participant.Formula)
                .Distinct()
        ];
        List<EquilibriumParticipant> participants =
        [
            .. reaction.Participants.Select(participant =>
                new EquilibriumParticipant(participant.Formula, participant.Phase, participant.StoichiometricNumber, participant.Moles))
        ];
        List<EquilibriumParticipant> stoichiometric =
        [
            .. participants.Select(participant =>
                participant with { InitialMoles = participant.StoichiometricNumber < 0 ? -participant.StoichiometricNumber : 0d })
        ];
        var equilibrium = speciesWithoutEntropy.Count == 0
            && ChemicalEquilibrium.TryDescribe(participants, table, request.Kelvin, request.Kilopascals, out var state)
                ? state
                : null;
        var kinetics = KineticsOf(reaction, request, equilibrium);

        return new ReactionConditionsEntry(
            kinetics,
            equilibrium is null ? null : EquilibriumEntryOf(equilibrium, stoichiometric, request, table),
            request.IncludeCurves && (equilibrium is not null || kinetics is not null)
                ? CurveOf(stoichiometric, table, request.Kilopascals, equilibrium is not null, reaction.ActivationEnergyKilojoulesPerMole)
                : null,
            speciesWithoutEntropy);
    }

    private static KineticsEntry? KineticsOf(ConditionsReactionEntry reaction, ConditionsRequest request, EquilibriumState? equilibrium)
    {
        if (reaction.ActivationEnergyKilojoulesPerMole is not { } barrier)
        {
            return null;
        }

        var gaseousReactant = reaction.Participants
            .Select((participant, index) => participant.StoichiometricNumber < 0 && (equilibrium?.Phases[index] ?? participant.Phase) == Phase.Gas)
            .Any(gaseous => gaseous);

        return new KineticsEntry(
            Arrhenius.Log10RateFactor(barrier, request.Kelvin, ReferenceKelvin),
            Arrhenius.Log10ShareOverBarrier(barrier, request.Kelvin),
            gaseousReactant ? GasCollisions.Log10FrequencyFactor(request.Kelvin, request.Kilopascals, ReferenceKelvin, ReferenceKilopascals) : null);
    }

    private static EquilibriumEntry EquilibriumEntryOf(
        EquilibriumState state,
        IReadOnlyList<EquilibriumParticipant> stoichiometric,
        ConditionsRequest request,
        StandardStateTable table) =>
        new(
            state.EnthalpyKjPerMol,
            state.EntropyJPerMolKelvin,
            state.GibbsKjPerMol,
            state.Log10EquilibriumConstant,
            request.IncludeCurves ? CrossoverKelvinOf(stoichiometric, table, request) : null,
            state.Phases,
            state.Conversion,
            state.MolesAtEquilibrium,
            HalfConversionKelvinOf(stoichiometric, table, request));

    private static decimal? CrossoverKelvinOf(IReadOnlyList<EquilibriumParticipant> participants, StandardStateTable table, ConditionsRequest request)
    {
        double? GibbsAt(decimal kelvin) =>
            ChemicalEquilibrium.TryStandardChange(participants, table, kelvin, request.Kilopascals, out var change)
                ? (double)change.GibbsKjPerMolAt(kelvin)
                : null;

        return NearestCrossing(request.Kelvin, GibbsAt, 0d);
    }

    private static decimal? HalfConversionKelvinOf(IReadOnlyList<EquilibriumParticipant> stoichiometric, StandardStateTable table, ConditionsRequest request)
    {
        double? ConversionAt(decimal kelvin) =>
            ChemicalEquilibrium.TryDescribe(stoichiometric, table, kelvin, request.Kilopascals, out var state) ? state.Conversion : null;

        return ConversionAt(request.Kelvin) < HalfConversion ? NearestCrossing(request.Kelvin, ConversionAt, HalfConversion) : null;
    }

    private static decimal? NearestCrossing(decimal kelvin, Func<decimal, double?> valueAt, double level)
    {
        var values = CurveKelvins.Select(valueAt).ToArray();
        (decimal Low, decimal High)? nearest = null;

        for (var point = 1; point < CurveKelvins.Length; point++)
        {
            var crosses = values[point - 1] is { } before && values[point] is { } after && (before < level) != (after < level);

            if (crosses && (nearest is not { } chosen
                || DistanceTo(kelvin, CurveKelvins[point - 1], CurveKelvins[point]) < DistanceTo(kelvin, chosen.Low, chosen.High)))
            {
                nearest = (CurveKelvins[point - 1], CurveKelvins[point]);
            }
        }

        return nearest is { } bracket ? Bisect(bracket.Low, bracket.High, valueAt, level) : null;
    }

    private static decimal DistanceTo(decimal kelvin, decimal lowKelvin, decimal highKelvin)
    {
        if (kelvin < lowKelvin)
        {
            return lowKelvin - kelvin;
        }

        return kelvin > highKelvin ? kelvin - highKelvin : 0m;
    }

    private static decimal Bisect(decimal lowKelvin, decimal highKelvin, Func<decimal, double?> valueAt, double level)
    {
        var lowBelow = valueAt(lowKelvin) < level;

        for (var step = 0; step < CrossingBisections; step++)
        {
            var middle = (lowKelvin + highKelvin) / 2m;

            if ((valueAt(middle) < level) == lowBelow)
            {
                lowKelvin = middle;
            }
            else
            {
                highKelvin = middle;
            }
        }

        return (lowKelvin + highKelvin) / 2m;
    }

    private static ConditionsCurveEntry CurveOf(
        IReadOnlyList<EquilibriumParticipant> stoichiometric,
        StandardStateTable table,
        decimal kilopascals,
        bool withEquilibrium,
        decimal? activationEnergyKjPerMol) =>
        new(
            CurveKelvins,
            withEquilibrium ? ConversionsAcross(stoichiometric, table, kilopascals) : null,
            withEquilibrium ? ConversionsAcross(stoichiometric, table, kilopascals * CurvePressureMultiplier) : null,
            withEquilibrium ? ConversionsAcross(stoichiometric, table, kilopascals / CurvePressureMultiplier) : null,
            activationEnergyKjPerMol is { } barrier ? [.. CurveKelvins.Select(kelvin => Arrhenius.Log10ShareOverBarrier(barrier, kelvin))] : null);

    private static List<double?> ConversionsAcross(IReadOnlyList<EquilibriumParticipant> participants, StandardStateTable table, decimal kilopascals) =>
        [.. CurveKelvins.Select(kelvin => ChemicalEquilibrium.TryDescribe(participants, table, kelvin, kilopascals, out var state) ? state.Conversion : (double?)null)];
}

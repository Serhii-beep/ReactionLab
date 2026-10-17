using System.Diagnostics.CodeAnalysis;
using ReactionLab.Chemistry.Quantities;

namespace ReactionLab.Chemistry.Thermochemistry;

public static class ChemicalEquilibrium
{
    private const double StandardPressureKilopascals = 100d;
    private const double BoundaryInset = 1e-12;
    private const int Bisections = 200;

    public static bool TryDescribe(
        IReadOnlyList<EquilibriumParticipant> participants,
        StandardStateTable table,
        decimal kelvin,
        decimal kilopascals,
        [NotNullWhen(true)] out EquilibriumState? state)
    {
        state = null;

        if (!TryStandardChange(participants, table, kelvin, kilopascals, out var change))
        {
            return false;
        }

        var gibbs = change.GibbsKjPerMolAt(kelvin);
        var naturalLogK = -(double)gibbs * 1000d / ((double)IdealGas.GasConstant * (double)kelvin);
        var (minimum, maximum) = ExtentBounds(participants);
        var extent = EquilibriumExtent(participants, change.Phases, naturalLogK, (double)kilopascals / StandardPressureKilopascals, minimum, maximum);

        state = new EquilibriumState(
            change.EnthalpyKjPerMol,
            change.EntropyJPerMolKelvin,
            gibbs,
            naturalLogK / Math.Log(10d),
            change.Phases,
            extent,
            minimum,
            maximum,
            [.. participants.Select(participant => participant.InitialMoles + (participant.StoichiometricNumber * extent))]);

        return true;
    }

    public static bool TryStandardChange(
        IReadOnlyList<EquilibriumParticipant> participants,
        StandardStateTable table,
        decimal kelvin,
        decimal kilopascals,
        [NotNullWhen(true)] out StandardReactionChange? change)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kilopascals);

        change = null;
        var phases = new Phase[participants.Count];
        var enthalpy = 0m;
        var entropy = 0m;

        for (var index = 0; index < participants.Count; index++)
        {
            var participant = participants[index];
            var keepsPhase = participants.Count(other => string.Equals(other.Formula, participant.Formula, StringComparison.Ordinal)) > 1;

            if (!TryFindParticipant(table, participant, keepsPhase, kelvin, kilopascals, out phases[index], out var standard)
                || standard.StandardEntropyJPerMolKelvin is not { } speciesEntropy)
            {
                return false;
            }

            enthalpy += participant.StoichiometricNumber * standard.FormationEnthalpyKjPerMol;
            entropy += participant.StoichiometricNumber * speciesEntropy;
        }

        change = new StandardReactionChange(enthalpy, entropy, phases);

        return true;
    }

    private static bool TryFindParticipant(
        StandardStateTable table,
        EquilibriumParticipant participant,
        bool keepsPhase,
        decimal kelvin,
        decimal kilopascals,
        out Phase phase,
        out StandardState standard)
    {
        phase = participant.Phase;

        return keepsPhase
            ? table.TryFind(participant.Formula, participant.Phase, out standard)
            : table.TryFindStable(participant.Formula, participant.Phase, kelvin, kilopascals, out phase, out standard);
    }

    private static (double Minimum, double Maximum) ExtentBounds(IReadOnlyList<EquilibriumParticipant> participants)
    {
        var minimum = double.NegativeInfinity;
        var maximum = double.PositiveInfinity;

        foreach (var participant in participants)
        {
            if (participant.StoichiometricNumber > 0)
            {
                minimum = Math.Max(minimum, -participant.InitialMoles / participant.StoichiometricNumber);
            }
            else if (participant.StoichiometricNumber < 0)
            {
                maximum = Math.Min(maximum, participant.InitialMoles / -participant.StoichiometricNumber);
            }
        }

        return (double.IsFinite(minimum) ? minimum : 0d, double.IsFinite(maximum) ? maximum : 0d);
    }

    private static double EquilibriumExtent(
        IReadOnlyList<EquilibriumParticipant> participants,
        IReadOnlyList<Phase> phases,
        double naturalLogK,
        double pressureRatio,
        double minimum,
        double maximum)
    {
        var span = maximum - minimum;
        var gases = Enumerable.Range(0, participants.Count).Where(index => phases[index] == Phase.Gas).ToArray();

        if (span <= 0d)
        {
            return minimum;
        }

        if (gases.Length == 0)
        {
            return naturalLogK > 0d ? maximum : minimum;
        }

        double Mismatch(double extent) => NaturalLogQuotient(participants, gases, extent, pressureRatio) - naturalLogK;

        if (Mismatch(maximum - (span * BoundaryInset)) < 0d)
        {
            return maximum;
        }

        if (Mismatch(minimum + (span * BoundaryInset)) > 0d)
        {
            return minimum;
        }

        var low = minimum;
        var high = maximum;

        for (var step = 0; step < Bisections && high - low > span * BoundaryInset; step++)
        {
            var middle = (low + high) / 2d;

            if (Mismatch(middle) < 0d)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return (low + high) / 2d;
    }

    private static double NaturalLogQuotient(IReadOnlyList<EquilibriumParticipant> participants, int[] gases, double extent, double pressureRatio)
    {
        var totalMoles = gases.Sum(index => participants[index].InitialMoles + (participants[index].StoichiometricNumber * extent));
        var quotient = 0d;

        foreach (var index in gases)
        {
            var stoichiometricNumber = participants[index].StoichiometricNumber;

            if (stoichiometricNumber == 0)
            {
                continue;
            }

            var moles = participants[index].InitialMoles + (stoichiometricNumber * extent);

            quotient += stoichiometricNumber * Math.Log(moles / totalMoles * pressureRatio);
        }

        return quotient;
    }
}

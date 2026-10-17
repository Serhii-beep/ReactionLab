using ReactionLab.Chemistry.Ions;
using ReactionLab.Chemistry.Quantities;

namespace ReactionLab.Chemistry.Thermochemistry;

public sealed class StandardStateTable(
    IReadOnlyList<SpeciesStandardState> species,
    IReadOnlyList<AqueousIon> aqueousIons,
    IonTable table)
{
    private const decimal StandardPressureKilopascals = 100m;

    public bool TryFind(string formula, Phase phase, out StandardState state)
    {
        foreach (var candidate in species)
        {
            if (candidate.Phase == phase && string.Equals(candidate.Formula, formula, StringComparison.Ordinal))
            {
                state = new StandardState(candidate.FormationEnthalpyKjPerMol, candidate.StandardEntropyJPerMolKelvin);

                return true;
            }
        }

        state = default;

        return phase == Phase.Aqueous && TryDissolved(formula, out state);
    }

    public bool TryFindStable(string formula, Phase phase, decimal kelvin, decimal kilopascals, out Phase stablePhase, out StandardState state)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kelvin);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(kilopascals);

        stablePhase = phase;

        if (!TryFind(formula, phase, out state))
        {
            return false;
        }

        if (phase == Phase.Aqueous || state.StandardEntropyJPerMolKelvin is not { } ownEntropy)
        {
            return true;
        }

        var least = MolarGibbsEnergy(phase, state.FormationEnthalpyKjPerMol, ownEntropy, kelvin, kilopascals);

        foreach (var candidate in species)
        {
            if (candidate.Phase is Phase.Aqueous
                || candidate.StandardEntropyJPerMolKelvin is not { } entropy
                || !string.Equals(candidate.Formula, formula, StringComparison.Ordinal))
            {
                continue;
            }

            var gibbs = MolarGibbsEnergy(candidate.Phase, candidate.FormationEnthalpyKjPerMol, entropy, kelvin, kilopascals);

            if (gibbs < least)
            {
                least = gibbs;
                stablePhase = candidate.Phase;
                state = new StandardState(candidate.FormationEnthalpyKjPerMol, entropy);
            }
        }

        return true;
    }

    public bool HasEntropy(string formula, Phase phase) =>
        species.Any(candidate => candidate.Phase == phase
            && candidate.StandardEntropyJPerMolKelvin is not null
            && string.Equals(candidate.Formula, formula, StringComparison.Ordinal));

    private static double MolarGibbsEnergy(Phase phase, decimal enthalpyKjPerMol, decimal entropyJPerMolKelvin, decimal kelvin, decimal kilopascals)
    {
        var standard = (double)ReactionEnergetics.GibbsFreeEnergy(enthalpyKjPerMol, entropyJPerMolKelvin, kelvin);

        return phase == Phase.Gas
            ? standard + ((double)IdealGas.GasConstant * (double)kelvin * Math.Log((double)(kilopascals / StandardPressureKilopascals)) / 1000d)
            : standard;
    }

    private bool TryDissolved(string formula, out StandardState state)
    {
        state = default;

        if (table.TrySplit(formula, out var cation, out var anion))
        {
            if (!TryIon(cation, out var cationEnthalpy) || !TryIon(anion, out var anionEnthalpy))
            {
                return false;
            }

            var divisor = Divisor(cation.Magnitude, anion.Magnitude);
            state = new StandardState(
                anion.Magnitude / divisor * cationEnthalpy + cation.Magnitude / divisor * anionEnthalpy, null);

            return true;
        }

        if (!table.TryReadAcid(formula, out var acidAnion) || !TryIon(acidAnion, out var acidEnthalpy))
        {
            return false;
        }

        state = new StandardState(acidEnthalpy, null);

        return true;
    }

    private bool TryIon(Ion ion, out decimal enthalpy)
    {
        foreach (var candidate in aqueousIons)
        {
            if (candidate.Ion == ion)
            {
                enthalpy = candidate.FormationEnthalpyKjPerMol;

                return true;
            }
        }

        enthalpy = 0m;

        return false;
    }

    private static int Divisor(int first, int second) =>
        second == 0 ? first : Divisor(second, first % second);
}

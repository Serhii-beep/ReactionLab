using ReactionLab.Chemistry.Ions;

namespace ReactionLab.Chemistry.Thermochemistry;

public sealed class StandardStateTable(
    IReadOnlyList<SpeciesEnthalpy> species,
    IReadOnlyList<AqueousIon> aqueousIons,
    IonTable table)
{
    public bool TryFind(string formula, Phase phase, out StandardState state)
    {
        foreach (var candidate in species)
        {
            if (candidate.Phase == phase && string.Equals(candidate.Formula, formula, StringComparison.Ordinal))
            {
                state = new StandardState(candidate.FormationEnthalpyKjPerMol, null);

                return true;
            }
        }

        state = default;

        return phase == Phase.Aqueous && TryDissolved(formula, out state);
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

using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.Chemistry.Valence;

public static class ValenceAnalysis
{
    private const int Octet = 8;
    private const int Duet = 2;
    private const int FirstExpandingPeriod = 3;

    public static IReadOnlyList<AtomValence> Analyze(
        IReadOnlyList<string> symbols,
        IReadOnlyList<int?> formalCharges,
        IReadOnlyList<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table,
        IReadOnlyDictionary<string, int> periods)
    {
        if (symbols.Count != formalCharges.Count)
        {
            throw new ArgumentException("Every atom needs a formal charge entry.", nameof(formalCharges));
        }

        var shells = new ValenceShell?[symbols.Count];
        var bondOrderSums = new int[symbols.Count];

        foreach (var bond in bonds)
        {
            bondOrderSums[bond.From] += bond.Order;
            bondOrderSums[bond.To] += bond.Order;
        }

        for (var atom = 0; atom < symbols.Count; atom++)
        {
            shells[atom] = table.TryGetValue(symbols[atom], out var geometry)
                && periods.TryGetValue(symbols[atom], out var period)
                ? new ValenceShell(geometry.ValenceElectrons, period, bondOrderSums[atom])
                : null;
        }

        var charges = ChargeBalancing.Assign(shells, formalCharges, bonds);

        return
        [
            .. Enumerable.Range(0, symbols.Count).Select(atom => shells[atom] is { } shell
                ? Describe(shell, charges[atom], formalCharges[atom] is null && charges[atom] != 0)
                : new AtomValence(formalCharges[atom] ?? 0, false, 0, 0, ValenceStatus.Unchecked))
        ];
    }

    internal static (ValenceStatus Status, int OpenValence) Judge(ValenceShell shell, int formalCharge)
    {
        var owned = shell.ValenceElectrons - formalCharge;
        var bonded = shell.BondOrderSum;

        if (owned < 0)
        {
            return (ValenceStatus.TooManyBonds, 0);
        }

        if (shell.Period == 1)
        {
            return Against(owned == 0 ? 0 : Duet - owned, bonded);
        }

        var octetBonds = owned <= 3 ? owned : Octet - owned;

        if (shell.Period < FirstExpandingPeriod || bonded <= octetBonds)
        {
            return Against(octetBonds, bonded);
        }

        if (bonded > owned)
        {
            return (ValenceStatus.TooManyBonds, 0);
        }

        return (owned - bonded) % 2 == 0 ? (ValenceStatus.Complete, 0) : (ValenceStatus.NeedsBonds, 1);
    }

    private static AtomValence Describe(ValenceShell shell, int formalCharge, bool inferred)
    {
        var (status, openValence) = Judge(shell, formalCharge);
        var unshared = shell.ValenceElectrons - formalCharge - shell.BondOrderSum;

        return new AtomValence(formalCharge, inferred, Math.Max(0, unshared - openValence) / 2, openValence, status);
    }

    private static (ValenceStatus Status, int OpenValence) Against(int targetBonds, int bonded)
    {
        if (targetBonds < 0 || bonded > targetBonds)
        {
            return (ValenceStatus.TooManyBonds, 0);
        }

        return bonded == targetBonds ? (ValenceStatus.Complete, 0) : (ValenceStatus.NeedsBonds, targetBonds - bonded);
    }
}

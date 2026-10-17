using ReactionLab.Chemistry.Geometry;

namespace ReactionLab.Chemistry.Valence;

internal static class ChargeBalancing
{
    private const int Octet = 8;
    private const int FirstExpandingPeriod = 3;
    private const int TetravalentValenceElectrons = 4;

    public static int[] Assign(ValenceShell?[] shells, IReadOnlyList<int?> explicitCharges, IReadOnlyList<AtomBond> bonds)
    {
        var charges = new int[shells.Length];

        for (var atom = 0; atom < shells.Length; atom++)
        {
            charges[atom] = explicitCharges[atom] ?? Overbonded(shells[atom]);
        }

        var surplus = Enumerable.Range(0, shells.Length)
            .Where(atom => explicitCharges[atom] is null)
            .Sum(atom => charges[atom]);

        if (surplus <= 0)
        {
            return charges;
        }

        var distances = DistancesFromCations(charges, explicitCharges, bonds);
        var acceptors = Enumerable.Range(0, shells.Length)
            .Where(atom => explicitCharges[atom] is null && charges[atom] == 0 && AcceptsAnElectron(shells[atom])
                && MayCarryTheCharge(atom, shells, distances, bonds))
            .OrderByDescending(atom => shells[atom]!.Value.ValenceElectrons)
            .ThenBy(atom => shells[atom]!.Value.Period)
            .ThenBy(atom => distances[atom])
            .ThenBy(atom => atom)
            .Take(surplus);

        foreach (var acceptor in acceptors)
        {
            charges[acceptor] = -1;
        }

        return charges;
    }

    private static int Overbonded(ValenceShell? shell)
    {
        if (shell is not { Period: > 1 } atom)
        {
            return 0;
        }

        var neutralBonds = atom.ValenceElectrons <= 3 || atom.Period >= FirstExpandingPeriod
            ? atom.ValenceElectrons
            : Octet - atom.ValenceElectrons;

        if (atom.BondOrderSum <= neutralBonds)
        {
            return 0;
        }

        var charge = atom.Period < FirstExpandingPeriod
            ? atom.ValenceElectrons - Octet + atom.BondOrderSum
            : atom.ValenceElectrons - atom.BondOrderSum;

        return Math.Abs(charge) == 1 && ValenceAnalysis.Judge(atom, charge).Status == ValenceStatus.Complete
            ? charge
            : 0;
    }

    private static bool AcceptsAnElectron(ValenceShell? shell) =>
        shell is { } atom
        && ValenceAnalysis.Judge(atom, 0) == (ValenceStatus.NeedsBonds, 1)
        && ValenceAnalysis.Judge(atom, -1).Status == ValenceStatus.Complete;

    private static bool MayCarryTheCharge(int atom, ValenceShell?[] shells, int[] distances, IReadOnlyList<AtomBond> bonds) =>
        shells[atom]!.Value.ValenceElectrons != TetravalentValenceElectrons
        || (distances[atom] == 1 && bonds.Count(bond => bond.From == atom || bond.To == atom) == 1);

    private static int[] DistancesFromCations(int[] charges, IReadOnlyList<int?> explicitCharges, IReadOnlyList<AtomBond> bonds)
    {
        var distances = new int[charges.Length];
        Array.Fill(distances, int.MaxValue);
        var queue = new Queue<int>();

        for (var atom = 0; atom < charges.Length; atom++)
        {
            if (explicitCharges[atom] is null && charges[atom] > 0)
            {
                distances[atom] = 0;
                queue.Enqueue(atom);
            }
        }

        while (queue.TryDequeue(out var atom))
        {
            foreach (var bond in bonds.Where(bond => bond.From == atom || bond.To == atom))
            {
                var neighbor = bond.From == atom ? bond.To : bond.From;

                if (distances[neighbor] == int.MaxValue)
                {
                    distances[neighbor] = distances[atom] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return distances;
    }
}

namespace ReactionLab.Chemistry.Geometry;

internal static class ConjugatedElectronDomains
{
    public static ElectronDomains?[] Of(
        IReadOnlyList<SkeletalAtom> atoms,
        IReadOnlyList<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        var bondOrders = new List<int>[atoms.Count];
        var neighbors = new List<int>[atoms.Count];

        for (var atom = 0; atom < atoms.Count; atom++)
        {
            bondOrders[atom] = [];
            neighbors[atom] = [];
        }

        foreach (var bond in bonds)
        {
            bondOrders[bond.From].Add(bond.Order);
            bondOrders[bond.To].Add(bond.Order);
            neighbors[bond.From].Add(bond.To);
            neighbors[bond.To].Add(bond.From);
        }

        var domains = new ElectronDomains?[atoms.Count];

        for (var atom = 0; atom < atoms.Count; atom++)
        {
            domains[atom] = Around(atoms[atom], bondOrders[atom], table);
        }

        var inPiSystem = Array.ConvertAll(bondOrders, orders => orders.Exists(order => order >= 2));

        for (var atom = 0; atom < atoms.Count; atom++)
        {
            if (domains[atom] is { StericNumber: 4, LonePairs: > 0 } tetrahedral
                && !inPiSystem[atom]
                && neighbors[atom].Exists(neighbor => inPiSystem[neighbor] && domains[neighbor] is { StericNumber: <= 3 }))
            {
                domains[atom] = new ElectronDomains(tetrahedral.BondingDomains, tetrahedral.LonePairs - 1);
            }
        }

        return domains;
    }

    private static ElectronDomains? Around(
        SkeletalAtom atom, List<int> bondOrders, IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        if (!table.TryGetValue(atom.Symbol, out var geometry))
        {
            return null;
        }

        try
        {
            return ElectronDomains.Around(geometry.ValenceElectrons, bondOrders, atom.FormalCharge);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}

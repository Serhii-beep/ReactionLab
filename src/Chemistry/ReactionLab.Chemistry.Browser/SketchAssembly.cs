using System.Numerics;
using ReactionLab.Chemistry.Geometry;
using ReactionLab.Chemistry.Valence;

namespace ReactionLab.Chemistry.Browser;

internal static class SketchAssembly
{
    private const string Hydrogen = "H";
    private const float FragmentGapAngstroms = 2f;

    public static SketchGeometry Of(
        IReadOnlyList<string> symbols,
        IReadOnlyList<AtomValence> valences,
        IReadOnlyList<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        if (symbols.Count == 0)
        {
            return SketchGeometry.Empty;
        }

        if (valences.FirstOrDefault(valence => valence.Status is ValenceStatus.Unchecked or ValenceStatus.TooManyBonds)
            is { Status: ValenceStatus.Unchecked or ValenceStatus.TooManyBonds } refused)
        {
            return SketchGeometry.Failed(refused.Status == ValenceStatus.Unchecked
                ? nameof(AssemblyError.UnknownElement)
                : nameof(ValenceStatus.TooManyBonds));
        }

        var atoms = symbols.Select((symbol, atom) => new SkeletalAtom(symbol, valences[atom].FormalCharge)).ToList();
        var completed = bonds.ToList();
        var owners = new List<int>();

        for (var atom = 0; atom < symbols.Count; atom++)
        {
            for (var open = 0; open < valences[atom].OpenValence; open++)
            {
                owners.Add(atom);
                completed.Add(new AtomBond(atom, atoms.Count, 1));
                atoms.Add(new SkeletalAtom(Hydrogen));
            }
        }

        var positions = new Vector3[atoms.Count];
        var fragments = Fragments(atoms.Count, completed);
        var offset = 0f;

        foreach (var fragment in fragments)
        {
            if (!TryAssemble(fragment, atoms, completed, table, out var placed, out var error))
            {
                return SketchGeometry.Failed(error.ToString());
            }

            var left = placed.Min(position => position.X);
            var width = placed.Max(position => position.X) - left;

            for (var member = 0; member < fragment.Count; member++)
            {
                positions[fragment[member]] = placed[member] + new Vector3(offset - left, 0f, 0f);
            }

            offset += width + FragmentGapAngstroms;
        }

        return new SketchGeometry(positions[..symbols.Count], owners, positions[symbols.Count..], fragments.Count, null);
    }

    private static bool TryAssemble(
        List<int> fragment,
        List<SkeletalAtom> atoms,
        List<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table,
        out IReadOnlyList<Vector3> placed,
        out AssemblyError error)
    {
        var localIndex = new Dictionary<int, int>();

        for (var member = 0; member < fragment.Count; member++)
        {
            localIndex[fragment[member]] = member;
        }

        return MoleculeAssembler.TryAssemble(
            [.. fragment.Select(atom => atoms[atom])],
            [.. bonds
                .Where(bond => localIndex.ContainsKey(bond.From))
                .Select(bond => new AtomBond(localIndex[bond.From], localIndex[bond.To], bond.Order))],
            table,
            out placed,
            out error);
    }

    private static List<List<int>> Fragments(int count, List<AtomBond> bonds)
    {
        var neighbors = new List<int>[count];

        for (var atom = 0; atom < count; atom++)
        {
            neighbors[atom] = [];
        }

        foreach (var bond in bonds)
        {
            neighbors[bond.From].Add(bond.To);
            neighbors[bond.To].Add(bond.From);
        }

        var seen = new bool[count];
        var fragments = new List<List<int>>();

        for (var start = 0; start < count; start++)
        {
            if (seen[start])
            {
                continue;
            }

            var fragment = new List<int> { start };
            seen[start] = true;

            for (var head = 0; head < fragment.Count; head++)
            {
                foreach (var neighbor in neighbors[fragment[head]].Where(neighbor => !seen[neighbor]))
                {
                    seen[neighbor] = true;
                    fragment.Add(neighbor);
                }
            }

            fragments.Add(fragment);
        }

        return fragments;
    }
}

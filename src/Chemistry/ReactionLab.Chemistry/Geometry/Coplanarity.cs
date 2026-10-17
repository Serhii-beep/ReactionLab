using System.Numerics;

namespace ReactionLab.Chemistry.Geometry;

public static class Coplanarity
{
    private const float TrigonalCenterWeight = 1f;
    private const float ConjugatedBondWeight = 0.5f;
    private const float EclipsingRotorWeight = 0.2f;
    private const float SmallestBaseArea = 0.25f;
    private const int SmallestPuckeredRing = 8;

    public static IReadOnlyList<CoplanarityTarget> Targets(
        IReadOnlyList<SkeletalAtom> atoms,
        IReadOnlyList<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        var neighbors = new List<int>[atoms.Count];

        for (var atom = 0; atom < atoms.Count; atom++)
        {
            neighbors[atom] = [];
        }

        foreach (var bond in bonds)
        {
            neighbors[bond.From].Add(bond.To);
            neighbors[bond.To].Add(bond.From);
        }

        var domains = ConjugatedElectronDomains.Of(atoms, bonds, table);
        var trigonal = Array.ConvertAll(domains, atom => atom?.StericNumber == 3);
        var rotors = Enumerable.Range(0, atoms.Count)
            .Select(atom => domains[atom]?.StericNumber == 4 && IsSymmetricRotor(atom, atoms, neighbors))
            .ToArray();
        var planarWithoutPiBond = (bool[])trigonal.Clone();

        foreach (var bond in bonds.Where(bond => bond.Order >= 2))
        {
            planarWithoutPiBond[bond.From] = false;
            planarWithoutPiBond[bond.To] = false;
        }

        return
        [
            .. CenterTargets(neighbors, trigonal),
            .. ConjugationTargets(bonds, neighbors, trigonal),
            .. EclipsingTargets(bonds, neighbors, planarWithoutPiBond, rotors)
        ];
    }

    public static double Strain(IReadOnlyList<Vector3> positions, IReadOnlyList<CoplanarityTarget> targets)
    {
        var total = 0d;

        foreach (var target in targets)
        {
            var origin = positions[target.First];
            var baseNormal = Vector3.Cross(positions[target.Second] - origin, positions[target.Third] - origin);
            var baseArea = MathF.Max(baseNormal.LengthSquared(), SmallestBaseArea);
            var volume = Vector3.Dot(positions[target.Fourth] - origin, baseNormal);

            total += target.Weight * (double)volume * volume / baseArea;
        }

        return total;
    }

    internal static void AddGradient(
        Vector3[] positions, CoplanarityTarget[] targets, float emphasis, Vector3[] gradient)
    {
        foreach (var target in targets)
        {
            var origin = positions[target.First];
            var second = positions[target.Second] - origin;
            var third = positions[target.Third] - origin;
            var fourth = positions[target.Fourth] - origin;

            var towardFourth = Vector3.Cross(second, third);
            var baseArea = MathF.Max(towardFourth.LengthSquared(), SmallestBaseArea);
            var towardSecond = Vector3.Cross(third, fourth);
            var towardThird = Vector3.Cross(fourth, second);
            var pull = emphasis * target.Weight * 2f * Vector3.Dot(fourth, towardFourth) / baseArea;

            gradient[target.Second] += pull * towardSecond;
            gradient[target.Third] += pull * towardThird;
            gradient[target.Fourth] += pull * towardFourth;
            gradient[target.First] -= pull * (towardSecond + towardThird + towardFourth);
        }
    }

    private static IEnumerable<CoplanarityTarget> CenterTargets(List<int>[] neighbors, bool[] trigonal)
    {
        for (var center = 0; center < neighbors.Length; center++)
        {
            if (trigonal[center] && neighbors[center].Count == 3)
            {
                yield return new CoplanarityTarget(
                    center, neighbors[center][0], neighbors[center][1], neighbors[center][2], TrigonalCenterWeight);
            }
        }
    }

    private static IEnumerable<CoplanarityTarget> EclipsingTargets(
        IReadOnlyList<AtomBond> bonds, List<int>[] neighbors, bool[] planar, bool[] rotors)
    {
        foreach (var bond in bonds)
        {
            if (planar[bond.From] && rotors[bond.To] && neighbors[bond.From].Count > 1)
            {
                yield return Eclipsing(bond.From, bond.To, neighbors);
            }
            else if (planar[bond.To] && rotors[bond.From] && neighbors[bond.To].Count > 1)
            {
                yield return Eclipsing(bond.To, bond.From, neighbors);
            }
        }
    }

    private static CoplanarityTarget Eclipsing(int planar, int rotor, List<int>[] neighbors) =>
        new(
            planar,
            rotor,
            neighbors[planar].Find(atom => atom != rotor),
            neighbors[rotor].Find(atom => atom != planar),
            EclipsingRotorWeight);

    private static bool IsSymmetricRotor(int atom, IReadOnlyList<SkeletalAtom> atoms, List<int>[] neighbors)
    {
        var terminals = neighbors[atom].Where(neighbor => neighbors[neighbor].Count == 1).ToList();

        return terminals.Count >= 2
            && terminals.Count == neighbors[atom].Count - 1
            && terminals.TrueForAll(terminal =>
                string.Equals(atoms[terminal].Symbol, atoms[terminals[0]].Symbol, StringComparison.Ordinal));
    }

    private static IEnumerable<CoplanarityTarget> ConjugationTargets(
        IReadOnlyList<AtomBond> bonds, List<int>[] neighbors, bool[] trigonal)
    {
        foreach (var bond in bonds.Where(bond => trigonal[bond.From] && trigonal[bond.To] && !InPuckeredRing(bond, neighbors)))
        {
            var before = neighbors[bond.From].FindIndex(atom => atom != bond.To);
            var after = neighbors[bond.To].FindIndex(atom => atom != bond.From);

            if (before >= 0 && after >= 0)
            {
                yield return new CoplanarityTarget(
                    bond.From, bond.To, neighbors[bond.From][before], neighbors[bond.To][after], ConjugatedBondWeight);
            }
        }
    }

    private static bool InPuckeredRing(AtomBond bond, List<int>[] neighbors)
    {
        var steps = new int[neighbors.Length];
        Array.Fill(steps, -1);
        steps[bond.From] = 0;
        var queue = new Queue<int>([bond.From]);

        while (queue.TryDequeue(out var atom))
        {
            foreach (var next in neighbors[atom].Where(next => steps[next] < 0 && !(atom == bond.From && next == bond.To)))
            {
                steps[next] = steps[atom] + 1;
                queue.Enqueue(next);
            }
        }

        return steps[bond.To] + 1 >= SmallestPuckeredRing;
    }
}

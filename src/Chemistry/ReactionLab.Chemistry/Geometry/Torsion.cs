using System.Numerics;

namespace ReactionLab.Chemistry.Geometry;

public static class Torsion
{
    private const float StaggeringWeight = 0.05f;
    private const float SkewWeight = 0.05f;
    private const float EclipsingWeight = 0.03f;
    private const float AntiWeight = 0.02f;
    private const float CumuleneWeight = 0.5f;
    private const float SmallestArmArea = 1e-4f;
    private const int SkewingLonePairs = 2;

    public static IReadOnlyList<TorsionTarget> Targets(
        IReadOnlyList<SkeletalAtom> atoms,
        IReadOnlyList<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        var neighbors = new List<(int Atom, int Order)>[atoms.Count];

        for (var atom = 0; atom < atoms.Count; atom++)
        {
            neighbors[atom] = [];
        }

        foreach (var bond in bonds)
        {
            neighbors[bond.From].Add((bond.To, bond.Order));
            neighbors[bond.To].Add((bond.From, bond.Order));
        }

        var domains = ConjugatedElectronDomains.Of(atoms, bonds, table);
        var heavy = atoms.Select(atom => !string.Equals(atom.Symbol, "H", StringComparison.Ordinal)).ToArray();
        var targets = new List<TorsionTarget>();

        foreach (var bond in bonds.Where(bond => bond.Order == 1))
        {
            AddBondTargets(targets, bond, neighbors, domains, heavy);
        }

        AddCumuleneTargets(targets, neighbors, domains);

        return targets;
    }

    public static double Strain(IReadOnlyList<Vector3> positions, IReadOnlyList<TorsionTarget> targets)
    {
        var total = 0d;

        foreach (var target in targets)
        {
            if (Dihedral(positions[target.First], positions[target.Second], positions[target.Third], positions[target.Fourth]) is { } angle)
            {
                total += target.Weight * (1d - Math.Cos(target.Periodicity * (angle - target.PreferredRadians))) / 2d;
            }
        }

        return total;
    }

    internal static void AddGradient(Vector3[] positions, TorsionTarget[] targets, Vector3[] gradient)
    {
        foreach (var target in targets)
        {
            var inbound = positions[target.Second] - positions[target.First];
            var axis = positions[target.Third] - positions[target.Second];
            var outbound = positions[target.Fourth] - positions[target.Third];
            var nearNormal = Vector3.Cross(inbound, axis);
            var farNormal = Vector3.Cross(axis, outbound);
            var nearArea = nearNormal.LengthSquared();
            var farArea = farNormal.LengthSquared();

            if (nearArea < SmallestArmArea || farArea < SmallestArmArea)
            {
                continue;
            }

            var axisLength = axis.Length();
            var angle = MathF.Atan2(axisLength * Vector3.Dot(inbound, farNormal), Vector3.Dot(nearNormal, farNormal));
            var slope = target.Weight * target.Periodicity * MathF.Sin(target.Periodicity * (angle - target.PreferredRadians)) / 2f;
            var towardFirst = -axisLength / nearArea * nearNormal;
            var towardFourth = axisLength / farArea * farNormal;
            var inboundShare = Vector3.Dot(inbound, axis) / (axisLength * axisLength);
            var outboundShare = Vector3.Dot(outbound, axis) / (axisLength * axisLength);

            gradient[target.First] += slope * towardFirst;
            gradient[target.Fourth] += slope * towardFourth;
            gradient[target.Second] += slope * (((-1f - inboundShare) * towardFirst) + (outboundShare * towardFourth));
            gradient[target.Third] += slope * ((inboundShare * towardFirst) - ((1f + outboundShare) * towardFourth));
        }
    }

    private static void AddBondTargets(
        List<TorsionTarget> targets,
        AtomBond bond,
        List<(int Atom, int Order)>[] neighbors,
        ElectronDomains?[] domains,
        bool[] heavy)
    {
        if (domains[bond.From] is not { } from || domains[bond.To] is not { } to)
        {
            return;
        }

        var before = OthersAround(bond.From, bond.To, neighbors);
        var after = OthersAround(bond.To, bond.From, neighbors);

        if (before.Count == 0 || after.Count == 0 || before.Exists(after.Contains))
        {
            return;
        }

        switch ((from.StericNumber, to.StericNumber))
        {
            case (4, 4):
                AddRotorTargets(targets, before, bond, after, from.LonePairs >= SkewingLonePairs && to.LonePairs >= SkewingLonePairs);
                break;
            case (4, 3):
                AddEclipsingTargets(targets, before, bond.From, bond.To, neighbors);
                break;
            case (3, 4):
                AddEclipsingTargets(targets, after, bond.To, bond.From, neighbors);
                break;
            default:
                return;
        }

        AddAntiTargets(targets, before, bond, after, neighbors, heavy);
    }

    private static void AddRotorTargets(List<TorsionTarget> targets, List<int> before, AtomBond bond, List<int> after, bool skewed)
    {
        var weight = (skewed ? SkewWeight : StaggeringWeight) / (before.Count * after.Count);

        foreach (var left in before)
        {
            foreach (var right in after)
            {
                targets.Add(skewed
                    ? new TorsionTarget(left, bond.From, bond.To, right, 2, MathF.PI / 2f, weight)
                    : new TorsionTarget(left, bond.From, bond.To, right, 3, MathF.PI, weight));
            }
        }
    }

    private static void AddEclipsingTargets(
        List<TorsionTarget> targets, List<int> substituents, int tetrahedral, int trigonal, List<(int Atom, int Order)>[] neighbors)
    {
        var partner = neighbors[trigonal].FindIndex(link => link.Atom != tetrahedral && link.Order >= 2);

        if (partner < 0)
        {
            return;
        }

        foreach (var substituent in substituents)
        {
            targets.Add(new TorsionTarget(
                substituent, tetrahedral, trigonal, neighbors[trigonal][partner].Atom, 3, 0f, EclipsingWeight / substituents.Count));
        }
    }

    private static void AddAntiTargets(
        List<TorsionTarget> targets,
        List<int> before,
        AtomBond bond,
        List<int> after,
        List<(int Atom, int Order)>[] neighbors,
        bool[] heavy)
    {
        foreach (var left in before.Where(atom => heavy[atom]))
        {
            foreach (var right in after.Where(atom => heavy[atom] && !Connected(left, atom, bond, neighbors)))
            {
                targets.Add(new TorsionTarget(left, bond.From, bond.To, right, 1, MathF.PI, AntiWeight));
            }
        }
    }

    private static void AddCumuleneTargets(
        List<TorsionTarget> targets, List<(int Atom, int Order)>[] neighbors, ElectronDomains?[] domains)
    {
        for (var end = 0; end < neighbors.Length; end++)
        {
            if (domains[end]?.StericNumber != 3)
            {
                continue;
            }

            foreach (var (first, _) in neighbors[end].Where(link => link.Order == 2 && IsCumulated(link.Atom, neighbors)))
            {
                var (previous, last, doubleBonds) = FarEndOf(end, first, neighbors);

                if (last > end && domains[last]?.StericNumber == 3)
                {
                    AddCumulenePairs(targets, end, OthersAround(end, first, neighbors), last, OthersAround(last, previous, neighbors), doubleBonds);
                }
            }
        }
    }

    private static void AddCumulenePairs(List<TorsionTarget> targets, int end, List<int> before, int last, List<int> after, int doubleBonds)
    {
        var preferred = doubleBonds % 2 == 0 ? MathF.PI / 2f : 0f;

        foreach (var left in before)
        {
            foreach (var right in after)
            {
                targets.Add(new TorsionTarget(left, end, last, right, 2, preferred, CumuleneWeight / (before.Count * after.Count)));
            }
        }
    }

    private static (int Previous, int Last, int DoubleBonds) FarEndOf(int end, int first, List<(int Atom, int Order)>[] neighbors)
    {
        var previous = end;
        var current = first;
        var doubleBonds = 1;

        for (var step = 0; step < neighbors.Length && IsCumulated(current, neighbors); step++)
        {
            var next = neighbors[current].Find(link => link.Atom != previous).Atom;
            previous = current;
            current = next;
            doubleBonds++;
        }

        return (previous, current, doubleBonds);
    }

    private static bool IsCumulated(int atom, List<(int Atom, int Order)>[] neighbors) =>
        neighbors[atom].Count == 2 && neighbors[atom].TrueForAll(link => link.Order == 2);

    private static List<int> OthersAround(int atom, int excluded, List<(int Atom, int Order)>[] neighbors) =>
        [.. neighbors[atom].Where(link => link.Atom != excluded).Select(link => link.Atom)];

    private static bool Connected(int start, int goal, AtomBond bond, List<(int Atom, int Order)>[] neighbors)
    {
        var seen = new bool[neighbors.Length];
        seen[start] = seen[bond.From] = seen[bond.To] = true;
        var queue = new Queue<int>([start]);

        while (queue.TryDequeue(out var atom))
        {
            foreach (var (next, _) in neighbors[atom])
            {
                if (next == goal)
                {
                    return true;
                }

                if (!seen[next])
                {
                    seen[next] = true;
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    private static float? Dihedral(Vector3 first, Vector3 second, Vector3 third, Vector3 fourth)
    {
        var inbound = second - first;
        var axis = third - second;
        var outbound = fourth - third;
        var nearNormal = Vector3.Cross(inbound, axis);
        var farNormal = Vector3.Cross(axis, outbound);

        if (nearNormal.LengthSquared() < SmallestArmArea || farNormal.LengthSquared() < SmallestArmArea)
        {
            return null;
        }

        return MathF.Atan2(axis.Length() * Vector3.Dot(inbound, farNormal), Vector3.Dot(nearNormal, farNormal));
    }
}

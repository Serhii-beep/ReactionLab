using System.Numerics;

namespace ReactionLab.Chemistry.Geometry;

public static class GeometryRelaxation
{
    private const double LonePairCompressionDegrees = 2.5d;

    private const int Passes = 3000;
    private const float Step = 0.02f;
    private const float Momentum = 0.9f;
    private const double Settled = 0.01d;

    public static IReadOnlyList<DistanceTarget> Targets(
        IReadOnlyList<SkeletalAtom> atoms,
        IReadOnlyList<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        var neighbours = Adjacency(atoms.Count, bonds);
        var aromatic = AromaticBonds(atoms, bonds, neighbours, table);
        var targets = new List<DistanceTarget>();
        var lengths = new Dictionary<(int, int), float>();

        foreach (var bond in bonds)
        {
            var length = BondLength(atoms, table, bond, aromatic);
            lengths[(bond.From, bond.To)] = length;
            lengths[(bond.To, bond.From)] = length;
            targets.Add(new DistanceTarget(bond.From, bond.To, length, 1f, false));
        }

        for (var centre = 0; centre < atoms.Count; centre++)
        {
            if (neighbours[centre].Count < 2 || IdealAngle(atoms[centre], neighbours[centre], table) is not { } angle)
            {
                continue;
            }

            for (var first = 0; first < neighbours[centre].Count; first++)
            {
                for (var second = first + 1; second < neighbours[centre].Count; second++)
                {
                    var left = lengths[(centre, neighbours[centre][first].Atom)];
                    var right = lengths[(centre, neighbours[centre][second].Atom)];

                    var across = Math.Sqrt(
                        (left * left) + (right * right)
                        - (2 * left * right * Math.Cos(double.DegreesToRadians(angle))));

                    targets.Add(new DistanceTarget(
                        neighbours[centre][first].Atom, neighbours[centre][second].Atom,
                        (float)across, 0.6f, false));
                }
            }
        }

        var constrained = targets
            .Select(target => (Math.Min(target.From, target.To), Math.Max(target.From, target.To)))
            .ToHashSet();

        for (var first = 0; first < atoms.Count; first++)
        {
            for (var second = first + 1; second < atoms.Count; second++)
            {
                if (constrained.Contains((first, second)))
                {
                    continue;
                }

                var floor = 1.8f * (table[atoms[first].Symbol].RadiusFor(1)
                    + table[atoms[second].Symbol].RadiusFor(1)) / 100f;

                targets.Add(new DistanceTarget(first, second, floor, 0.15f, true));
            }
        }

        return targets;
    }

    public static IReadOnlyList<Vector3> Settle(
        IReadOnlyList<Vector3> start, IReadOnlyList<DistanceTarget> targets, int attempts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attempts);

        var best = Minimize(start, targets, 0);
        var least = Strain(best, targets);

        for (var seed = 1; seed < attempts && least > Settled; seed++)
        {
            var candidate = Minimize(start, targets, seed);
            var strain = Strain(candidate, targets);

            if (strain < least)
            {
                best = candidate;
                least = strain;
            }
        }

        return best;
    }

    public static double Strain(IReadOnlyList<Vector3> positions, IReadOnlyList<DistanceTarget> targets)
    {
        var total = 0d;

        foreach (var target in targets)
        {
            var distance = Vector3.Distance(positions[target.From], positions[target.To]);

            if (!target.FloorOnly || distance < target.Distance)
            {
                total += target.Weight * Math.Pow(distance - target.Distance, 2);
            }
        }

        return total;
    }

    private static Vector3[] Minimize(
        IReadOnlyList<Vector3> start, IReadOnlyList<DistanceTarget> targets, int seed)
    {
        var current = start.ToArray();
        var gradient = new Vector3[current.Length];
        var velocity = new Vector3[current.Length];

        if (seed != 0)
        {
            var random = new Random(seed);

            for (var i = 0; i < current.Length; i++)
            {
                current[i] += new Vector3(
                    (float)(random.NextDouble() - 0.5) * 2f,
                    (float)(random.NextDouble() - 0.5) * 2f,
                    (float)(random.NextDouble() - 0.5) * 2f);
            }
        }

        for (var pass = 0; pass < Passes; pass++)
        {
            Array.Clear(gradient);
            var step = Step * (1f - (0.9f * pass / Passes));

            foreach (var target in targets)
            {
                var delta = current[target.From] - current[target.To];
                var distance = delta.Length();

                if (distance < 1e-6f || (target.FloorOnly && distance >= target.Distance))
                {
                    continue;
                }

                var pull = target.Weight * (distance - target.Distance) * 2f / distance;
                gradient[target.From] += pull * delta;
                gradient[target.To] -= pull * delta;
            }

            for (var i = 0; i < current.Length; i++)
            {
                velocity[i] = (Momentum * velocity[i]) - (step * gradient[i]);
                current[i] += velocity[i];
            }
        }

        return current;
    }

    private static float BondLength(
        IReadOnlyList<SkeletalAtom> atoms,
        IReadOnlyDictionary<string, AtomicGeometry> table,
        AtomBond bond,
        HashSet<(int, int)> aromatic) =>
        aromatic.Contains((Math.Min(bond.From, bond.To), Math.Max(bond.From, bond.To)))
            ? (table[atoms[bond.From].Symbol].AromaticRadius
                + table[atoms[bond.To].Symbol].AromaticRadius) / 100f
            : (table[atoms[bond.From].Symbol].RadiusFor(bond.Order)
                + table[atoms[bond.To].Symbol].RadiusFor(bond.Order)) / 100f;

    private static HashSet<(int, int)> AromaticBonds(
        IReadOnlyList<SkeletalAtom> atoms,
        IReadOnlyList<AtomBond> bonds,
        List<(int Atom, int Order)>[] neighbours,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        var found = new HashSet<(int, int)>();
        var trigonal = new bool[atoms.Count];

        for (var i = 0; i < atoms.Count; i++)
        {
            trigonal[i] = StericNumber(atoms[i], neighbours[i], table) == 3;
        }

        foreach (var bond in bonds)
        {
            if (trigonal[bond.From]
                && trigonal[bond.To]
                && SmallestRingThrough(bond, neighbours, atoms.Count) is { } ring
                && ring.TrueForAll(atom => trigonal[atom]))
            {
                found.Add((Math.Min(bond.From, bond.To), Math.Max(bond.From, bond.To)));
            }
        }

        return found;
    }

    private static List<int>? SmallestRingThrough(
        AtomBond bond, List<(int Atom, int Order)>[] neighbours, int count)
    {
        var previous = new int[count];
        Array.Fill(previous, -2);
        previous[bond.From] = -1;
        var queue = new List<int> { bond.From };

        for (var head = 0; head < queue.Count; head++)
        {
            foreach (var (next, _) in neighbours[queue[head]])
            {
                if ((queue[head] == bond.From && next == bond.To) || previous[next] != -2)
                {
                    continue;
                }

                previous[next] = queue[head];

                if (next != bond.To)
                {
                    queue.Add(next);
                    continue;
                }

                var ring = new List<int>();

                for (var at = bond.To; at != -1; at = previous[at])
                {
                    ring.Add(at);
                }

                return ring.Count <= 7 ? ring : null;
            }
        }

        return null;
    }

    private static double? IdealAngle(
        SkeletalAtom atom,
        List<(int Atom, int Order)> neighbours,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        try
        {
            var domains = ElectronDomains.Around(
                table[atom.Symbol].ValenceElectrons,
                [.. neighbours.Select(neighbour => neighbour.Order)],
                atom.FormalCharge);

            var directions = Vsepr.Directions(domains);

            if (directions.Count < 2)
            {
                return null;
            }

            var ideal = double.RadiansToDegrees(Math.Acos(Math.Clamp(
                Vector3.Dot(Vector3.Normalize(directions[0]), Vector3.Normalize(directions[1])), -1f, 1f)));

            return ideal - (LonePairCompressionDegrees * domains.LonePairs);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static int StericNumber(
        SkeletalAtom atom,
        List<(int Atom, int Order)> neighbours,
        IReadOnlyDictionary<string, AtomicGeometry> table)
    {
        try
        {
            return ElectronDomains.Around(
                table[atom.Symbol].ValenceElectrons,
                [.. neighbours.Select(neighbour => neighbour.Order)],
                atom.FormalCharge).StericNumber;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return -1;
        }
    }

    private static List<(int Atom, int Order)>[] Adjacency(int count, IReadOnlyList<AtomBond> bonds)
    {
        var neighbours = new List<(int Atom, int Order)>[count];

        for (var i = 0; i < count; i++)
        {
            neighbours[i] = [];
        }

        foreach (var bond in bonds)
        {
            neighbours[bond.From].Add((bond.To, bond.Order));
            neighbours[bond.To].Add((bond.From, bond.Order));
        }

        return neighbours;
    }
}

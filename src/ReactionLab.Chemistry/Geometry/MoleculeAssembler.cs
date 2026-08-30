using System.Numerics;

namespace ReactionLab.Chemistry.Geometry;

public static class MoleculeAssembler
{
    public static bool TryAssemble(
        IReadOnlyList<SkeletalAtom> atoms,
        IReadOnlyList<AtomBond> bonds,
        IReadOnlyDictionary<string, AtomicGeometry> table,
        out IReadOnlyList<Vector3> positions,
        out AssemblyError error)
    {
        positions = [];
        var count = atoms.Count;

        if (count == 0)
        {
            error = AssemblyError.NoAtoms;
            return false;
        }

        foreach (var atom in atoms)
        {
            if (!table.ContainsKey(atom.Symbol))
            {
                error = AssemblyError.UnknownElement;
                return false;
            }
        }

        var neighbours = Adjacency(count, bonds);
        var parent = new int[count];
        Array.Fill(parent, -1);
        var visit = BreadthFirst(count, neighbours, parent);

        if (visit.Count != count)
        {
            error = AssemblyError.Disconnected;
            return false;
        }

        var placed = new Vector3[count];

        foreach (var atom in visit)
        {
            if (!TryShapeOf(atoms[atom], neighbours[atom], table, out var directions))
            {
                error = AssemblyError.ShapeUnavailable;
                return false;
            }

            var frame = parent[atom] < 0
                ? directions
                : Turned(directions, Vector3.Normalize(placed[parent[atom]] - placed[atom]));

            var slot = parent[atom] < 0 ? 0 : 1;

            var positioned = new bool[count];
            positioned[0] = true;

            foreach (var (child, order) in neighbours[atom])
            {
                if (child == parent[atom] || positioned[child] || slot >= frame.Count)
                {
                    continue;
                }

                var length = table[atoms[atom].Symbol].RadiusFor(order)
                    + table[atoms[child].Symbol].RadiusFor(order);

                placed[child] = placed[atom] + (frame[slot++] * (length / 100f));
                positioned[child] = true;
            }
        }

        positions = GeometryRelaxation.Settle(placed, GeometryRelaxation.Targets(atoms, bonds, table), 8);
        error = AssemblyError.None;
        return true;
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

    private static List<int> BreadthFirst(
        int count, List<(int Atom, int Order)>[] neighbours, int[] parent)
    {
        var seen = new bool[count];
        var visit = new List<int>(count) { 0 };
        seen[0] = true;

        for (var head = 0; head < visit.Count; head++)
        {
            foreach (var (neighbour, _) in neighbours[visit[head]])
            {
                if (!seen[neighbour])
                {
                    seen[neighbour] = true;
                    parent[neighbour] = visit[head];
                    visit.Add(neighbour);
                }
            }
        }

        return visit;
    }

    private static bool TryShapeOf(
        SkeletalAtom atom,
        List<(int Atom, int Order)> neighbours,
        IReadOnlyDictionary<string, AtomicGeometry> table,
        out IReadOnlyList<Vector3> directions)
    {
        directions = [];

        try
        {
            directions = Vsepr.Directions(ElectronDomains.Around(
                table[atom.Symbol].ValenceElectrons,
                [.. neighbours.Select(neighbour => neighbour.Order)],
                atom.FormalCharge));

            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private static IReadOnlyList<Vector3> Turned(IReadOnlyList<Vector3> directions, Vector3 toParent) =>
        [.. directions.Select(direction =>
            Vector3.Transform(direction, RotationTaking(directions[0], toParent)))];

    private static Quaternion RotationTaking(Vector3 from, Vector3 to)
    {
        var dot = Math.Clamp(Vector3.Dot(from, to), -1f, 1f);

        if (dot > 0.999999f)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.999999f)
        {
            var axis = Vector3.Cross(from, Vector3.UnitX);

            if (axis.LengthSquared() < 1e-6f)
            {
                axis = Vector3.Cross(from, Vector3.UnitY);
            }

            return Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), MathF.PI);
        }

        return Quaternion.CreateFromAxisAngle(
            Vector3.Normalize(Vector3.Cross(from, to)), MathF.Acos(dot));
    }
}

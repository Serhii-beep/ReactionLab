using System.Numerics;

namespace ReactionLab.Chemistry.Symmetry;

internal sealed class SymmetryElementSearch
{
    private const float ToleranceAngstroms = 0.15f;
    private const float ParallelCosine = 0.9986f;
    private const int HighestTestedOrder = 8;

    private readonly string[] _symbols;
    private readonly Vector3[] _positions;

    public SymmetryElementSearch(IReadOnlyList<string> symbols, IReadOnlyList<Vector3> centeredPositions)
    {
        _symbols = [.. symbols];
        _positions = [.. centeredPositions];
    }

    public IReadOnlyList<Vector3> CandidateDirections(IEnumerable<Vector3> principalAxes, bool sphericalTop)
    {
        var candidates = new List<Vector3>();

        foreach (var position in _positions)
        {
            AddDirection(candidates, position);
        }

        foreach (var (first, second) in EquivalentPairs())
        {
            AddDirection(candidates, _positions[first] + _positions[second]);
            AddDirection(candidates, _positions[first] - _positions[second]);
            AddDirection(candidates, Vector3.Cross(_positions[first], _positions[second]));
        }

        if (sphericalTop)
        {
            AddTriangleNormals(candidates);
        }

        candidates.AddRange(principalAxes);

        return Distinct(candidates);
    }

    public int RotationOrderAbout(Vector3 axis)
    {
        for (var order = HighestTestedOrder; order >= 2; order--)
        {
            if (IsSymmetry(Matrix4x4.CreateFromAxisAngle(axis, 2f * MathF.PI / order)))
            {
                return order;
            }
        }

        return 1;
    }

    public bool IsMirrorNormal(Vector3 normal) => IsSymmetry(Matrix4x4.CreateReflection(new Plane(normal, 0f)));

    public bool HasInversionCenter() => IsSymmetry(Matrix4x4.CreateScale(-1f));

    public bool HasRotoreflection(Vector3 axis, int order) =>
        IsSymmetry(Matrix4x4.CreateFromAxisAngle(axis, MathF.PI / order) * Matrix4x4.CreateReflection(new Plane(axis, 0f)));

    private bool IsSymmetry(Matrix4x4 operation)
    {
        for (var atom = 0; atom < _positions.Length; atom++)
        {
            if (!HasAtomAt(Vector3.Transform(_positions[atom], operation), _symbols[atom]))
            {
                return false;
            }
        }

        return true;
    }

    private bool HasAtomAt(Vector3 position, string symbol)
    {
        for (var atom = 0; atom < _positions.Length; atom++)
        {
            if (Vector3.DistanceSquared(_positions[atom], position) < ToleranceAngstroms * ToleranceAngstroms
                && string.Equals(_symbols[atom], symbol, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<(int First, int Second)> EquivalentPairs()
    {
        for (var first = 0; first < _positions.Length; first++)
        {
            for (var second = first + 1; second < _positions.Length; second++)
            {
                if (AreEquivalentCandidates(first, second))
                {
                    yield return (first, second);
                }
            }
        }
    }

    private void AddTriangleNormals(List<Vector3> candidates)
    {
        var shell = SmallestShell();

        for (var first = 0; first < shell.Count; first++)
        {
            for (var second = first + 1; second < shell.Count; second++)
            {
                for (var third = second + 1; third < shell.Count; third++)
                {
                    AddDirection(candidates, Vector3.Cross(
                        _positions[shell[second]] - _positions[shell[first]],
                        _positions[shell[third]] - _positions[shell[first]]));
                }
            }
        }
    }

    private List<int> SmallestShell()
    {
        var shells = new List<List<int>>();

        for (var atom = 0; atom < _positions.Length; atom++)
        {
            if (_positions[atom].Length() < ToleranceAngstroms)
            {
                continue;
            }

            var shell = shells.Find(members => AreEquivalentCandidates(members[0], atom));

            if (shell is null)
            {
                shells.Add([atom]);
            }
            else
            {
                shell.Add(atom);
            }
        }

        return shells.Where(shell => shell.Count >= 3).MinBy(shell => shell.Count) ?? [];
    }

    private bool AreEquivalentCandidates(int first, int second) =>
        string.Equals(_symbols[first], _symbols[second], StringComparison.Ordinal)
        && MathF.Abs(_positions[first].Length() - _positions[second].Length()) < ToleranceAngstroms;

    private static void AddDirection(List<Vector3> candidates, Vector3 vector)
    {
        if (vector.Length() > ToleranceAngstroms * 0.1f)
        {
            candidates.Add(Vector3.Normalize(vector));
        }
    }

    private static List<Vector3> Distinct(List<Vector3> candidates)
    {
        var distinct = new List<Vector3>();

        foreach (var candidate in candidates)
        {
            if (!distinct.Exists(kept => MathF.Abs(Vector3.Dot(kept, candidate)) >= ParallelCosine))
            {
                distinct.Add(candidate);
            }
        }

        return distinct;
    }
}

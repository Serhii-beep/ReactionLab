using System.Globalization;
using System.Numerics;

namespace ReactionLab.Chemistry.Symmetry;

public static class PointGroupDetector
{
    private const double LinearMoment = 0.01d;
    private const double SphericalSpread = 0.01d;
    private const float ParallelCosine = 0.9986f;
    private const float PerpendicularCosine = 0.0523f;
    private const float SameElementCosine = 0.990f;

    public static PointGroup Detect(IReadOnlyList<string> symbols, IReadOnlyList<Vector3> positionsAngstroms)
    {
        if (symbols.Count == 0 || symbols.Count != positionsAngstroms.Count)
        {
            throw new ArgumentException("Every atom needs a symbol and a position.", nameof(positionsAngstroms));
        }

        var center = Centroid(positionsAngstroms);

        if (symbols.Count == 1)
        {
            return new PointGroup("Kh", center, [], [], true);
        }

        Vector3[] centered = [.. positionsAngstroms.Select(position => position - center)];
        var search = new SymmetryElementSearch(symbols, centered);
        var principal = PrincipalAxes.Of(centered);

        if (principal.RelativeMoments[0] < LinearMoment)
        {
            return Linear(center, principal.Axes[0], search.HasInversionCenter());
        }

        var candidates = search.CandidateDirections(
            principal.Axes, principal.RelativeMoments[2] - principal.RelativeMoments[0] < SphericalSpread);

        RotationAxis[] axes =
        [
            .. Merged(
                candidates
                    .Select(direction => new RotationAxis(direction, search.RotationOrderAbout(direction)))
                    .Where(axis => axis.Order >= 2)
                    .OrderByDescending(axis => axis.Order),
                axis => axis.Direction)
        ];
        Vector3[] mirrorNormals = [.. Merged(candidates.Where(search.IsMirrorNormal), normal => normal)];
        var hasInversionCenter = search.HasInversionCenter();

        return new PointGroup(
            Classify(axes, mirrorNormals, hasInversionCenter, search),
            center,
            axes,
            mirrorNormals,
            hasInversionCenter);
    }

    private static PointGroup Linear(Vector3 center, Vector3 axis, bool hasInversionCenter)
    {
        RotationAxis[] axes = [new RotationAxis(axis, RotationAxis.InfiniteOrder)];

        return hasInversionCenter
            ? new PointGroup("D∞h", center, axes, [axis], true)
            : new PointGroup("C∞v", center, axes, [], false);
    }

    private static string Classify(
        RotationAxis[] axes, Vector3[] mirrorNormals, bool hasInversionCenter, SymmetryElementSearch search)
    {
        if (axes.Count(axis => axis.Order >= 3) >= 2)
        {
            return Polyhedral(axes, mirrorNormals, hasInversionCenter);
        }

        if (axes.Length == 0)
        {
            return Unrotated(mirrorNormals, hasInversionCenter);
        }

        var principal = axes[0];
        var order = principal.Order.ToString(CultureInfo.InvariantCulture);
        var hasHorizontalMirror = mirrorNormals.Any(normal => IsParallel(normal, principal.Direction));

        if (axes.Any(axis => axis.Order == 2 && IsPerpendicular(axis.Direction, principal.Direction)))
        {
            return "D" + order + DihedralSuffix(hasHorizontalMirror, mirrorNormals.Length > 0);
        }

        if (hasHorizontalMirror)
        {
            return "C" + order + "h";
        }

        if (mirrorNormals.Any(normal => IsPerpendicular(normal, principal.Direction)))
        {
            return "C" + order + "v";
        }

        return search.HasRotoreflection(principal.Direction, principal.Order)
            ? "S" + (2 * principal.Order).ToString(CultureInfo.InvariantCulture)
            : "C" + order;
    }

    private static string Polyhedral(RotationAxis[] axes, Vector3[] mirrorNormals, bool hasInversionCenter)
    {
        if (axes.Any(axis => axis.Order == 5))
        {
            return hasInversionCenter ? "Ih" : "I";
        }

        if (axes.Any(axis => axis.Order == 4))
        {
            return hasInversionCenter ? "Oh" : "O";
        }

        if (hasInversionCenter)
        {
            return "Th";
        }

        return mirrorNormals.Length > 0 ? "Td" : "T";
    }

    private static string Unrotated(Vector3[] mirrorNormals, bool hasInversionCenter)
    {
        if (mirrorNormals.Length > 0)
        {
            return "Cs";
        }

        return hasInversionCenter ? "Ci" : "C1";
    }

    private static string DihedralSuffix(bool hasHorizontalMirror, bool hasMirror)
    {
        if (hasHorizontalMirror)
        {
            return "h";
        }

        return hasMirror ? "d" : string.Empty;
    }

    private static List<T> Merged<T>(IEnumerable<T> elements, Func<T, Vector3> directionOf)
    {
        var kept = new List<T>();

        foreach (var element in elements)
        {
            if (!kept.Exists(existing => MathF.Abs(Vector3.Dot(directionOf(existing), directionOf(element))) >= SameElementCosine))
            {
                kept.Add(element);
            }
        }

        return kept;
    }

    private static Vector3 Centroid(IReadOnlyList<Vector3> positions)
    {
        var sum = Vector3.Zero;

        foreach (var position in positions)
        {
            sum += position;
        }

        return sum / positions.Count;
    }

    private static bool IsParallel(Vector3 first, Vector3 second) =>
        MathF.Abs(Vector3.Dot(first, second)) >= ParallelCosine;

    private static bool IsPerpendicular(Vector3 first, Vector3 second) =>
        MathF.Abs(Vector3.Dot(first, second)) <= PerpendicularCosine;
}

using System.Numerics;

namespace ReactionLab.Chemistry.Geometry;

public static class Superposition
{
    private const int JacobiSweeps = 64;

    public static bool TryBestRootMeanSquareDeviation(
        IReadOnlyList<Vector3> first,
        IReadOnlyList<Vector3> second,
        IReadOnlyList<IReadOnlyList<int>> interchangeable,
        out double deviation)
    {
        deviation = 0d;

        if (first.Count != second.Count || first.Count == 0)
        {
            return false;
        }

        var working = second.ToArray();
        var best = double.MaxValue;

        Search(0);
        deviation = best;

        return true;

        void Search(int group)
        {
            if (group == interchangeable.Count)
            {
                if (TryRootMeanSquareDeviation(first, working, out var candidate))
                {
                    best = Math.Min(best, candidate);
                }

                return;
            }

            var slots = interchangeable[group];
            var original = slots.Select(slot => working[slot]).ToArray();

            foreach (var order in Permutations(original))
            {
                for (var i = 0; i < slots.Count; i++)
                {
                    working[slots[i]] = order[i];
                }

                Search(group + 1);
            }

            for (var i = 0; i < slots.Count; i++)
            {
                working[slots[i]] = original[i];
            }
        }
    }

    public static bool TryRootMeanSquareDeviation(
        IReadOnlyList<Vector3> first,
        IReadOnlyList<Vector3> second,
        out double deviation)
    {
        deviation = 0d;

        if (first.Count != second.Count || first.Count == 0)
        {
            return false;
        }

        var left = Centred(first);
        var right = Centred(second);
        var residual = 0d;

        for (var i = 0; i < left.Count; i++)
        {
            residual += Dot(left[i], left[i]) + Dot(right[i], right[i]);
        }

        var key = KeyMatrix(Covariance(left, right));
        var largest = LargestEigenvalue(key);
        var squared = (residual - 2d * largest) / left.Count;

        deviation = Math.Sqrt(Math.Max(0d, squared));

        return true;
    }

    private static List<Vector3> Centred(IReadOnlyList<Vector3> points)
    {
        var centre = Vector3.Zero;

        foreach (var point in points)
        {
            centre += point;
        }

        centre /= points.Count;

        return [.. points.Select(point => point - centre)];
    }

    private static double[,] Covariance(List<Vector3> left, List<Vector3> right)
    {
        var covariance = new double[3, 3];

        for (var i = 0; i < left.Count; i++)
        {
            double[] a = [left[i].X, left[i].Y, left[i].Z];
            double[] b = [right[i].X, right[i].Y, right[i].Z];

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    covariance[row, column] += a[row] * b[column];
                }
            }
        }

        return covariance;
    }

    private static double[,] KeyMatrix(double[,] r) => new[,]
    {
        { r[0, 0] + r[1, 1] + r[2, 2], r[1, 2] - r[2, 1], r[2, 0] - r[0, 2], r[0, 1] - r[1, 0] },
        { r[1, 2] - r[2, 1], r[0, 0] - r[1, 1] - r[2, 2], r[0, 1] + r[1, 0], r[2, 0] + r[0, 2] },
        { r[2, 0] - r[0, 2], r[0, 1] + r[1, 0], -r[0, 0] + r[1, 1] - r[2, 2], r[1, 2] + r[2, 1] },
        { r[0, 1] - r[1, 0], r[2, 0] + r[0, 2], r[1, 2] + r[2, 1], -r[0, 0] - r[1, 1] + r[2, 2] }
    };

    private static double LargestEigenvalue(double[,] matrix)
    {
        for (var sweep = 0; sweep < JacobiSweeps; sweep++)
        {
            var offDiagonal = 0d;

            for (var row = 0; row < 4; row++)
            {
                for (var column = row + 1; column < 4; column++)
                {
                    offDiagonal += matrix[row, column] * matrix[row, column];
                }
            }

            if (offDiagonal < 1e-18d)
            {
                break;
            }

            for (var row = 0; row < 3; row++)
            {
                for (var column = row + 1; column < 4; column++)
                {
                    Rotate(matrix, row, column);
                }
            }
        }

        var largest = matrix[0, 0];

        for (var i = 1; i < 4; i++)
        {
            largest = Math.Max(largest, matrix[i, i]);
        }

        return largest;
    }

    private static void Rotate(double[,] matrix, int p, int q)
    {
        if (Math.Abs(matrix[p, q]) < 1e-18d)
        {
            return;
        }

        var theta = (matrix[q, q] - matrix[p, p]) / (2d * matrix[p, q]);
        var sign = theta >= 0d ? 1d : -1d;
        var t = sign / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1d));
        var c = 1d / Math.Sqrt(t * t + 1d);
        var s = t * c;

        for (var k = 0; k < 4; k++)
        {
            var kp = matrix[k, p];
            var kq = matrix[k, q];
            matrix[k, p] = c * kp - s * kq;
            matrix[k, q] = s * kp + c * kq;
        }

        for (var k = 0; k < 4; k++)
        {
            var pk = matrix[p, k];
            var qk = matrix[q, k];
            matrix[p, k] = c * pk - s * qk;
            matrix[q, k] = s * pk + c * qk;
        }
    }

    private static IEnumerable<Vector3[]> Permutations(Vector3[] items)
    {
        if (items.Length <= 1)
        {
            yield return items;

            yield break;
        }

        for (var i = 0; i < items.Length; i++)
        {
            var position = i;

            foreach (var tail in Permutations([.. items.Where((_, other) => other != position)]))
            {
                yield return [items[position], .. tail];
            }
        }
    }

    private static double Dot(Vector3 a, Vector3 b) =>
        (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
}

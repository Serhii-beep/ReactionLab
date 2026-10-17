using System.Numerics;

namespace ReactionLab.Chemistry.Symmetry;

internal sealed record PrincipalAxes(IReadOnlyList<double> RelativeMoments, IReadOnlyList<Vector3> Axes)
{
    private const int JacobiSweeps = 32;

    public static PrincipalAxes Of(IReadOnlyList<Vector3> centeredPositions)
    {
        var tensor = new double[3, 3];

        foreach (var position in centeredPositions)
        {
            double[] coordinates = [position.X, position.Y, position.Z];
            var squaredLength = position.LengthSquared();

            for (var row = 0; row < 3; row++)
            {
                for (var column = 0; column < 3; column++)
                {
                    tensor[row, column] -= coordinates[row] * coordinates[column];
                }

                tensor[row, row] += squaredLength;
            }
        }

        var eigenvectors = new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
        Diagonalize(tensor, eigenvectors);

        var trace = Math.Max(tensor[0, 0] + tensor[1, 1] + tensor[2, 2], double.Epsilon);
        var order = Enumerable.Range(0, 3).OrderBy(index => tensor[index, index]).ToArray();

        return new PrincipalAxes(
            [.. order.Select(index => tensor[index, index] / trace)],
            [.. order.Select(index => Vector3.Normalize(new Vector3(
                (float)eigenvectors[0, index], (float)eigenvectors[1, index], (float)eigenvectors[2, index])))]);
    }

    private static void Diagonalize(double[,] matrix, double[,] eigenvectors)
    {
        for (var sweep = 0; sweep < JacobiSweeps; sweep++)
        {
            var offDiagonal = (matrix[0, 1] * matrix[0, 1]) + (matrix[0, 2] * matrix[0, 2]) + (matrix[1, 2] * matrix[1, 2]);

            if (offDiagonal < 1e-20d)
            {
                return;
            }

            for (var p = 0; p < 2; p++)
            {
                for (var q = p + 1; q < 3; q++)
                {
                    Rotate(matrix, eigenvectors, p, q);
                }
            }
        }
    }

    private static void Rotate(double[,] matrix, double[,] eigenvectors, int p, int q)
    {
        if (Math.Abs(matrix[p, q]) < 1e-20d)
        {
            return;
        }

        var theta = (matrix[q, q] - matrix[p, p]) / (2d * matrix[p, q]);
        var t = Math.Sign(theta == 0d ? 1d : theta) / (Math.Abs(theta) + Math.Sqrt((theta * theta) + 1d));
        var cosine = 1d / Math.Sqrt((t * t) + 1d);
        var sine = t * cosine;

        for (var k = 0; k < 3; k++)
        {
            var kp = matrix[k, p];
            var kq = matrix[k, q];
            matrix[k, p] = (cosine * kp) - (sine * kq);
            matrix[k, q] = (sine * kp) + (cosine * kq);
        }

        for (var k = 0; k < 3; k++)
        {
            var pk = matrix[p, k];
            var qk = matrix[q, k];
            matrix[p, k] = (cosine * pk) - (sine * qk);
            matrix[q, k] = (sine * pk) + (cosine * qk);
        }

        for (var k = 0; k < 3; k++)
        {
            var kp = eigenvectors[k, p];
            var kq = eigenvectors[k, q];
            eigenvectors[k, p] = (cosine * kp) - (sine * kq);
            eigenvectors[k, q] = (sine * kp) + (cosine * kq);
        }
    }
}

using System.Numerics;
using ReactionLab.Chemistry.Geometry;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Geometry;

public sealed class GeometryRelaxationTests
{
    private static readonly Dictionary<string, AtomicGeometry> Table = new(StringComparer.Ordinal)
    {
        ["H"] = new(1, 31),
        ["C"] = new(4, 76, 67, 60, 70),
        ["N"] = new(5, 71, 60, 54, 66),
        ["O"] = new(6, 66, 57, 53, 66)
    };

    private static readonly Dictionary<string, AtomicGeometry> Hypervalent = new(StringComparer.Ordinal)
    {
        ["F"] = new(7, 57),
        ["Cl"] = new(7, 102, 99),
        ["S"] = new(6, 105, 94, 95),
        ["P"] = new(5, 107, 102, 94),
        ["Br"] = new(7, 120),
        ["Xe"] = new(8, 140)
    };

    [Fact]
    public void Targets_TurnsABondAngleIntoADistance()
    {
        var across = Targets(Water())
            .Single(target => target is { From: 1, To: 2, FloorOnly: false });

        across.Distance.ShouldBe(1.534f, 0.002f);
    }

    [Fact]
    public void Targets_GivesAnAllTrigonalRingItsAromaticLength()
    {
        var (atoms, bonds) = Benzene();

        var ring = Targets((atoms, bonds))
            .Where(target => !target.FloorOnly && target.From < 6 && target.To < 6)
            .Where(target => bonds.Any(bond =>
                (bond.From == target.From && bond.To == target.To)
                || (bond.From == target.To && bond.To == target.From)))
            .ToList();

        ring.Count.ShouldBe(6);
        ring.ShouldAllBe(target => Math.Abs(target.Distance - 1.40f) < 0.001f);
    }

    [Theory]
    [InlineData("PCl5", "P", "Cl", 5, "90 120 180")]
    [InlineData("SF4", "S", "F", 4, "90 120 180")]
    [InlineData("ClF3", "Cl", "F", 3, "90 180")]
    [InlineData("SF6", "S", "F", 6, "90 180")]
    [InlineData("BrF5", "Br", "F", 5, "90 180")]
    [InlineData("XeF4", "Xe", "F", 4, "90 180")]
    public void Targets_SeparateHypervalentPairsByTheirOwnAngles(
        string species, string centre, string ligand, int count, string expected)
    {
        List<SkeletalAtom> atoms = [new(centre)];
        atoms.AddRange(Enumerable.Repeat(new SkeletalAtom(ligand), count));
        List<AtomBond> bonds = [.. Enumerable.Range(1, count).Select(i => new AtomBond(0, i, 1))];

        var placed = GeometryRelaxation.Settle(
            Opened(atoms.Count), GeometryRelaxation.Targets(atoms, bonds, Hypervalent), 8);

        var angles = new List<double>();

        for (var first = 1; first <= count; first++)
        {
            for (var second = first + 1; second <= count; second++)
            {
                angles.Add(Math.Round(Angles.At(placed, 0, first, second) / 10d) * 10d);
            }
        }

        string.Join(' ', angles.Distinct().Order()).ShouldBe(expected, species);
    }

    [Fact]
    public void Targets_DoNotTreatAFourMemberedRingAsAromatic()
    {
        List<SkeletalAtom> atoms = [.. Enumerable.Repeat(new SkeletalAtom("C"), 4)];
        atoms.AddRange(Enumerable.Repeat(new SkeletalAtom("H"), 4));

        List<AtomBond> bonds = [];

        for (var carbon = 0; carbon < 4; carbon++)
        {
            bonds.Add(new AtomBond(carbon, (carbon + 1) % 4, carbon % 2 == 0 ? 2 : 1));
            bonds.Add(new AtomBond(carbon, carbon + 4, 1));
        }

        var ring = GeometryRelaxation.Targets(atoms, bonds, Table)
            .Where(target => !target.FloorOnly && target.From < 4 && target.To < 4)
            .Where(target => bonds.Any(bond =>
                (bond.From == target.From && bond.To == target.To)
                || (bond.From == target.To && bond.To == target.From)))
            .Select(target => Math.Round(target.Distance, 2))
            .Distinct()
            .Order()
            .ToList();

        ring.ShouldBe([1.34d, 1.52d]);
    }

    [Fact]
    public void Settle_ClosesARing()
    {
        var (atoms, bonds) = Benzene();
        var targets = Targets((atoms, bonds));

        var settled = GeometryRelaxation.Settle(Opened(atoms.Count), targets, 8);

        GeometryRelaxation.Strain(settled, targets).ShouldBeLessThan(0.01d);

        foreach (var bond in bonds.Where(bond => bond.From < 6 && bond.To < 6))
        {
            Vector3.Distance(settled[bond.From], settled[bond.To]).ShouldBe(1.40f, 0.05f);
        }
    }

    [Fact]
    public void Settle_EscapesTheMinimumASingleStartFallsInto()
    {
        var targets = Targets(Benzene());
        var start = Opened(12);

        var once = GeometryRelaxation.Strain(GeometryRelaxation.Settle(start, targets, 1), targets);
        var again = GeometryRelaxation.Strain(GeometryRelaxation.Settle(start, targets, 8), targets);

        once.ShouldBeGreaterThan(1d);
        again.ShouldBeLessThan(0.01d);
    }

    [Fact]
    public void Settle_GivesTheSameAnswerTwice()
    {
        var targets = Targets(Benzene());
        var start = Opened(12);

        GeometryRelaxation.Settle(start, targets, 8)
            .ShouldBe(GeometryRelaxation.Settle(start, targets, 8));
    }

    [Fact]
    public void Strain_IsZeroWhenEveryTargetIsMet()
    {
        DistanceTarget[] targets = [new(0, 1, 1f, 1f, false)];

        GeometryRelaxation.Strain([Vector3.Zero, Vector3.UnitX], targets).ShouldBe(0d, 1e-9d);
    }

    private static IReadOnlyList<DistanceTarget> Targets(
        (IReadOnlyList<SkeletalAtom> Atoms, IReadOnlyList<AtomBond> Bonds) molecule) =>
        GeometryRelaxation.Targets(molecule.Atoms, molecule.Bonds, Table);

    private static (IReadOnlyList<SkeletalAtom> Atoms, IReadOnlyList<AtomBond> Bonds) Water() =>
        ([new("O"), new("H"), new("H")], [new AtomBond(0, 1, 1), new AtomBond(0, 2, 1)]);

    private static (IReadOnlyList<SkeletalAtom> Atoms, IReadOnlyList<AtomBond> Bonds) Benzene()
    {
        List<SkeletalAtom> atoms = [.. Enumerable.Repeat(new SkeletalAtom("C"), 6)];
        atoms.AddRange(Enumerable.Repeat(new SkeletalAtom("H"), 6));

        List<AtomBond> bonds = [];

        for (var i = 0; i < 6; i++)
        {
            bonds.Add(new AtomBond(i, (i + 1) % 6, i % 2 == 0 ? 2 : 1));
            bonds.Add(new AtomBond(i, i + 6, 1));
        }

        return (atoms, bonds);
    }

    private static Vector3[] Opened(int count) =>
        [.. Enumerable.Range(0, count).Select(i => new Vector3(i * 0.9f, 0f, 0f))];
}

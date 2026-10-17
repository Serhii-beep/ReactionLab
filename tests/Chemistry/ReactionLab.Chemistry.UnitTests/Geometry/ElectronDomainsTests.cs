using System.Globalization;
using ReactionLab.Chemistry.Geometry;
using Shouldly;
using Xunit;

namespace ReactionLab.Chemistry.UnitTests.Geometry;

public sealed class ElectronDomainsTests
{
    [Theory]
    [InlineData("H2O", 6, "1 1", 0, 2, 2)]
    [InlineData("NH3", 5, "1 1 1", 0, 3, 1)]
    [InlineData("CH4", 4, "1 1 1 1", 0, 4, 0)]
    [InlineData("CO2", 4, "2 2", 0, 2, 0)]
    [InlineData("SO2", 6, "2 2", 0, 2, 1)]
    [InlineData("NH4+", 5, "1 1 1 1", 1, 4, 0)]
    [InlineData("H3O+", 6, "1 1 1", 1, 3, 1)]
    [InlineData("NO3-", 5, "2 1 1", 1, 3, 0)]
    [InlineData("XeF4", 8, "1 1 1 1", 0, 4, 2)]
    public void Around_CountsWhatTheValenceElectronsHaveLeft(
        string species, int valence, string bondOrders, int formalCharge, int bonding, int lonePairs) =>
        ElectronDomains.Around(valence, Orders(bondOrders), formalCharge)
            .ShouldBe(new ElectronDomains(bonding, lonePairs), species);

    [Fact]
    public void Around_RejectsAnUnpairedElectron() =>
        Should.Throw<ArgumentException>(() =>
            _ = ElectronDomains.Around(4, Orders("1 1 1"), 0));

    [Fact]
    public void Around_RejectsMoreBondsThanTheAtomHasElectronsFor() =>
        Should.Throw<ArgumentOutOfRangeException>(() =>
            _ = ElectronDomains.Around(1, Orders("1 1"), 0));

    private static int[] Orders(string bondOrders) =>
    [
        .. bondOrders.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(order => int.Parse(order, CultureInfo.InvariantCulture))
    ];
}

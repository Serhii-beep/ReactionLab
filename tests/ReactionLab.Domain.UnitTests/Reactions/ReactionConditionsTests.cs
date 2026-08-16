using ReactionLab.Domain.Reactions;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Reactions;

public sealed class ReactionConditionsTests
{
    [Fact]
    public void RequiresCatalyst_IsDerivedFromCatalystPresence()
    {
        var withCatalyst = ReactionConditions.Create(null, null, "Platinum").Value;
        var withoutCatalyst = ReactionConditions.Create(null, null, null).Value;

        withCatalyst.RequiredCatalyst.ShouldBeTrue();
        withoutCatalyst.RequiredCatalyst.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankCatalyst_IsTreatedAsAbsent(string catalyst)
    {
        var conditions = ReactionConditions.Create(null, null, catalyst).Value;

        conditions.Catalyst.ShouldBeNull();
        conditions.RequiredCatalyst.ShouldBeFalse();
    }

    [Fact]
    public void Catalyst_IsTrimmed()
    {
        ReactionConditions.Create(null, null, "   Iron   ").Value.Catalyst.ShouldBe("Iron");
    }

    [Fact]
    public void TooLongCatalyst_IsRejected()
    {
        ReactionConditions.Create(null, null, new string('x', ReactionConditions.MaximumCatalystLength + 1)).Error
            .ShouldBe(ReactionConditions.CatalystTooLong);
    }

    [Fact]
    public void Ambient_IsRoomTemperatureAtOneAtmosphere()
    {
        ReactionConditions.Ambient.Temperature!.Kelvin.ShouldBe(298.15m);
        ReactionConditions.Ambient.Pressure!.Atmospheres.ShouldBe(1m);
        ReactionConditions.Ambient.RequiredCatalyst.ShouldBeFalse();
    }
}

using ReactionLab.Domain.Reference;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Reference;

public sealed class ChemistryReferenceTests
{
    private static ReferenceKey Ions => ReferenceKey.Create("ions").Value;

    [Theory]
    [InlineData("ions")]
    [InlineData("activity-series")]
    [InlineData("atomic-geometry")]
    [InlineData("v2-thermodynamics")]
    public void ReferenceKey_AcceptsASlug(string value) =>
        ReferenceKey.Create(value).Value.Value.ShouldBe(value);

    [Theory]
    [InlineData("Ions")]
    [InlineData("-ions")]
    [InlineData("ions-")]
    [InlineData("activity--series")]
    [InlineData("activity series")]
    [InlineData("activity_series")]
    public void ReferenceKey_RefusesAnythingThatIsNotOne(string value) =>
        ReferenceKey.Create(value).Error.ShouldBe(ReferenceKey.Malformed);

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void ReferenceKey_RefusesAnEmptyKey(string? value) =>
        ReferenceKey.Create(value).Error.ShouldBe(ReferenceKey.Required);

    [Fact]
    public void ReferenceKey_RefusesAKeyLongerThanTheColumn() =>
        ReferenceKey.Create(new string('a', ReferenceKey.MaximumLength + 1))
            .Error.ShouldBe(ReferenceKey.TooLong);

    [Fact]
    public void Create_KeepsTheKeyAndPayload()
    {
        var reference = ChemistryReference.Create(Ions, "  {\"cations\":[]}  ").Value;

        reference.Key.Value.ShouldBe("ions");
        reference.Payload.ShouldBe("{\"cations\":[]}");
        reference.Id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Replace_SwapsThePayloadInPlace()
    {
        var reference = ChemistryReference.Create(Ions, "{\"cations\":[]}").Value;

        reference.Replace("{\"cations\":[{\"formula\":\"Na\"}]}").IsSuccess.ShouldBeTrue();

        reference.Payload.ShouldBe("{\"cations\":[{\"formula\":\"Na\"}]}");
        reference.Key.Value.ShouldBe("ions");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_RefusesAnEmptyPayload(string? payload) =>
        ChemistryReference.Create(Ions, payload).Error.ShouldBe(ChemistryReference.PayloadRequired);

    [Fact]
    public void Replace_RefusesAnEmptyPayloadAndLeavesTheOldOne()
    {
        var reference = ChemistryReference.Create(Ions, "{\"a\":1}").Value;

        reference.Replace(null).Error.ShouldBe(ChemistryReference.PayloadRequired);

        reference.Payload.ShouldBe("{\"a\":1}");
    }
}

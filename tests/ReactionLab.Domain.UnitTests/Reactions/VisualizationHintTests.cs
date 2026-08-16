using ReactionLab.Domain.Reactions;
using Shouldly;
using Xunit;

namespace ReactionLab.Domain.UnitTests.Reactions;

public sealed class VisualizationHintTests
{
    [Fact]
    public void Create_AcceptsAPresetAndDuration()
    {
        var hint = VisualizationHint.Create("combustion", 3000).Value;

        hint.PresetKey.ShouldBe("combustion");
        hint.DurationMilliseconds.ShouldBe(3000);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(VisualizationHint.MaximumDurationMilliseconds + 1)]
    public void Create_RejectsInvalidDurations(int duration)
    {
        VisualizationHint.Create("combustion", duration).Error
            .ShouldBe(VisualizationHint.InvalidDuration);
    }

    [Fact]
    public void Create_RejectsTooLongPresetKey()
    {
        VisualizationHint.Create(new string('x', VisualizationHint.MaximumPresetKeyLength + 1), null).Error
            .ShouldBe(VisualizationHint.PresetKeyTooLong);
    }
}

using System.Globalization;
using ReactionLab.Catalog.Application.Common.Abstractions;
using ReactionLab.Catalog.Infrastructure.Releases.Matching;
using Shouldly;
using Xunit;

namespace ReactionLab.Catalog.Infrastructure.UnitTests.Releases;

public sealed class CatalogReleaseRegistryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly Guid Retired = ReleaseIdAt(Now.AddDays(-2));

    private static readonly Guid Active = ReleaseIdAt(Now.AddDays(-1));

    [Fact]
    public void Classify_ReadsAHeldRelease() =>
        Registry(retiredReadableUntil: Now.AddHours(1)).Classify(Retired).ShouldBe(ReleaseAvailability.Readable);

    [Fact]
    public void Classify_SupersedesAHeldReleaseOnceItsGracePeriodEnds() =>
        Registry(retiredReadableUntil: Now).Classify(Retired).ShouldBe(ReleaseAvailability.Superseded);

    [Fact]
    public void Classify_WaitsForAReleaseNewerThanEveryKnownOne() =>
        Registry(retiredReadableUntil: Now).Classify(ReleaseIdAt(Now.AddMinutes(-20))).ShouldBe(ReleaseAvailability.Pending);

    [Theory]
    [InlineData(-3)]
    [InlineData(1)]
    public void Classify_DoesNotKnowAnOlderOrFutureRelease(int days) =>
        Registry(retiredReadableUntil: Now).Classify(ReleaseIdAt(Now.AddDays(days))).ShouldBe(ReleaseAvailability.Unknown);

    [Fact]
    public void Classify_DoesNotWaitForAnIdThatIsNotVersionSeven() =>
        Registry(retiredReadableUntil: Now).Classify(Guid.NewGuid()).ShouldBe(ReleaseAvailability.Unknown);

    private static CatalogReleaseRegistry Registry(DateTimeOffset retiredReadableUntil)
    {
        var registry = new CatalogReleaseRegistry(new Clock(Now));
        var index = ReactionMatchIndex.Build([]);

        registry.Publish(new RegistryState(
            Active,
            new Dictionary<Guid, HeldRelease>
            {
                [Retired] = new(index, 1, retiredReadableUntil),
                [Active] = new(index, 1, null)
            },
            new HashSet<Guid> { Retired, Active },
            Active,
            Now));

        return registry;
    }

    private static Guid ReleaseIdAt(DateTimeOffset moment)
    {
        var milliseconds = moment.ToUnixTimeMilliseconds().ToString("x12", CultureInfo.InvariantCulture);

        return Guid.Parse($"{milliseconds[..8]}-{milliseconds[8..]}-7000-8000-000000000001");
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

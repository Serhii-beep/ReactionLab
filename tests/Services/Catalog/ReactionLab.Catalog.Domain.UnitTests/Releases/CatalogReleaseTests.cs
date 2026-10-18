using ReactionLab.Catalog.Domain.Releases;
using Shouldly;
using Xunit;

namespace ReactionLab.Catalog.Domain.UnitTests.Releases;

public sealed class CatalogReleaseTests
{
    private static readonly DateTimeOffset Built = new(2026, 10, 18, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C397F66BADD82EBD")]
    [InlineData("c397f66badd82eb")]
    [InlineData("c397f66badd82ebz")]
    public void Build_RejectsAMalformedVersion(string? version) =>
        CatalogRelease.Build(version, answerVersion: 1, changeMark: 1, Built).Error.ShouldBe(CatalogRelease.VersionMalformed);

    [Fact]
    public void Build_StartsBuilt()
    {
        var release = CatalogRelease.Build("c397f66badd82ebd", answerVersion: 2, changeMark: 125, Built).Value;

        release.Status.ShouldBe(CatalogReleaseStatus.Built);
        release.AnswerVersion.ShouldBe(2);
        release.ChangeMark.ShouldBe(125);
        release.ActivatedAt.ShouldBeNull();
    }

    [Fact]
    public void Lifecycle_GoesFromBuiltToActiveToRetiredToDropped()
    {
        var release = Release();

        release.Activate(Built.AddMinutes(1)).IsSuccess.ShouldBeTrue();
        release.Status.ShouldBe(CatalogReleaseStatus.Active);

        release.Retire(Built.AddHours(1)).IsSuccess.ShouldBeTrue();
        release.Status.ShouldBe(CatalogReleaseStatus.Retired);
        release.RetiredAt.ShouldBe(Built.AddHours(1));

        release.DropSnapshot().IsSuccess.ShouldBeTrue();
        release.Status.ShouldBe(CatalogReleaseStatus.Dropped);
    }

    [Fact]
    public void Lifecycle_RefusesToSkipAStep()
    {
        var release = Release();

        release.Retire(Built).Error.ShouldBe(CatalogRelease.NotActive);
        release.DropSnapshot().Error.ShouldBe(CatalogRelease.NotRetired);

        release.Activate(Built);

        release.Activate(Built).Error.ShouldBe(CatalogRelease.NotBuilt);
        release.Abandon().Error.ShouldBe(CatalogRelease.NotBuilt);
    }

    [Fact]
    public void Abandon_DropsABuiltRelease()
    {
        var release = Release();

        release.Abandon().IsSuccess.ShouldBeTrue();
        release.Status.ShouldBe(CatalogReleaseStatus.Dropped);
    }

    [Fact]
    public void IsReadableAt_KeepsARetiredReleaseForItsGracePeriod()
    {
        var release = Release();
        var retired = Built.AddHours(2);

        release.Activate(Built);
        release.Retire(retired);

        release.IsReadableAt(retired + CatalogRelease.GracePeriod - TimeSpan.FromSeconds(1)).ShouldBeTrue();
        release.IsReadableAt(retired + CatalogRelease.GracePeriod).ShouldBeFalse();
    }

    private static CatalogRelease Release() =>
        CatalogRelease.Build("c397f66badd82ebd", answerVersion: 1, changeMark: 1, Built).Value;
}

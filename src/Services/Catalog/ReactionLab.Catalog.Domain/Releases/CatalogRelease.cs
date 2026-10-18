using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Domain.Releases;

public sealed class CatalogRelease : AggregateRoot<CatalogReleaseId>
{
    public const int VersionLength = 16;

    public static readonly Error VersionMalformed = Error.Validation(
        "CatalogRelease.VersionMalformed",
        $"A release version must be {VersionLength} lowercase hexadecimal characters.")
        .WithArgs(("length", VersionLength));

    private CatalogRelease(CatalogReleaseId id, string version, DateTimeOffset publishedAt) : base(id)
    {
        Version = version;
        PublishedAt = publishedAt;
    }

    private CatalogRelease()
    {

    }

    public string Version { get; private set; } = null!;

    public DateTimeOffset PublishedAt { get; private set; }

    public static Result<CatalogRelease> Publish(string? version, DateTimeOffset publishedAt) =>
        version is { Length: VersionLength } && version.All(char.IsAsciiHexDigitLower)
            ? new CatalogRelease(CatalogReleaseId.New(), version, publishedAt)
            : VersionMalformed;
}

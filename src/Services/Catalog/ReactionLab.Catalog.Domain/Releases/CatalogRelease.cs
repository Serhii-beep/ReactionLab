using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Domain.Releases;

public sealed class CatalogRelease : AggregateRoot<CatalogReleaseId>
{
    public const int VersionLength = 16;

    public static readonly TimeSpan GracePeriod = TimeSpan.FromHours(24);

    public static readonly Error VersionMalformed = Error.Validation(
        "CatalogRelease.VersionMalformed",
        $"A release version must be {VersionLength} lowercase hexadecimal characters.")
        .WithArgs(("length", VersionLength));

    public static readonly Error NotBuilt = Error.Conflict(
        "CatalogRelease.NotBuilt",
        "Only a built release can be activated.");

    public static readonly Error NotActive = Error.Conflict(
        "CatalogRelease.NotActive",
        "Only the active release can be retired.");

    public static readonly Error NotRetired = Error.Conflict(
        "CatalogRelease.NotRetired",
        "Only a retired release can have its snapshot dropped.");

    private CatalogRelease(
        CatalogReleaseId id,
        string version,
        int answerVersion,
        long changeMark,
        DateTimeOffset publishedAt) : base(id)
    {
        Version = version;
        AnswerVersion = answerVersion;
        ChangeMark = changeMark;
        PublishedAt = publishedAt;
        Status = CatalogReleaseStatus.Built;
    }

    private CatalogRelease()
    {

    }

    public string Version { get; private set; } = null!;

    public CatalogReleaseStatus Status { get; private set; }

    public int AnswerVersion { get; private set; }

    public long ChangeMark { get; private set; }

    public DateTimeOffset PublishedAt { get; private set; }

    public DateTimeOffset? ActivatedAt { get; private set; }

    public DateTimeOffset? RetiredAt { get; private set; }

    public static Result<CatalogRelease> Build(string? version, int answerVersion, long changeMark, DateTimeOffset builtAt) =>
        version is { Length: VersionLength } && version.All(char.IsAsciiHexDigitLower)
            ? new CatalogRelease(CatalogReleaseId.New(), version, answerVersion, changeMark, builtAt)
            : VersionMalformed;

    public Result Activate(DateTimeOffset activatedAt)
    {
        if (Status != CatalogReleaseStatus.Built)
        {
            return Result.Failure(NotBuilt);
        }

        Status = CatalogReleaseStatus.Active;
        ActivatedAt = activatedAt;

        return Result.Success();
    }

    public Result Retire(DateTimeOffset retiredAt)
    {
        if (Status != CatalogReleaseStatus.Active)
        {
            return Result.Failure(NotActive);
        }

        Status = CatalogReleaseStatus.Retired;
        RetiredAt = retiredAt;

        return Result.Success();
    }

    public Result Abandon()
    {
        if (Status != CatalogReleaseStatus.Built)
        {
            return Result.Failure(NotBuilt);
        }

        Status = CatalogReleaseStatus.Dropped;

        return Result.Success();
    }

    public Result DropSnapshot()
    {
        if (Status != CatalogReleaseStatus.Retired)
        {
            return Result.Failure(NotRetired);
        }

        Status = CatalogReleaseStatus.Dropped;

        return Result.Success();
    }

    public bool IsReadableAt(DateTimeOffset moment) =>
        Status is CatalogReleaseStatus.Built or CatalogReleaseStatus.Active
        || (Status == CatalogReleaseStatus.Retired && RetiredAt + GracePeriod > moment);
}

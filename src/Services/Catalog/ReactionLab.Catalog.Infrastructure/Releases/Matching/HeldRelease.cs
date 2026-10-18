namespace ReactionLab.Catalog.Infrastructure.Releases.Matching;

internal sealed record HeldRelease(ReactionMatchIndex Index, int AnswerVersion, DateTimeOffset? ReadableUntil)
{
    public bool IsReadableAt(DateTimeOffset moment) => ReadableUntil is not { } until || moment < until;
}

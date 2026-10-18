using ReactionLab.Catalog.Application.Features.Releases.Search;
using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Releases;

public static class ReleaseErrors
{
    public const int MaximumBench = 32;

    public static readonly Error TermNotCanonical = Error.Validation(
        "Release.TermNotCanonical",
        "The search term must be trimmed, NFC, lowercase and single-spaced.",
        field: "q");

    public static readonly Error TermLength = Error.Validation(
        "Release.TermLength",
        $"The search term must be {SearchKeys.MinimumLength} to {SearchKeys.MaximumLength} characters long.",
        field: "q")
        .WithArgs(("min", SearchKeys.MinimumLength), ("max", SearchKeys.MaximumLength));

    public static readonly Error SearchWithElement = Error.Validation(
        "Release.SearchWithElement",
        "Search by a term or list by an element, not both.",
        field: "element");

    public static readonly Error BenchNotCanonical = Error.Validation(
        "Release.BenchNotCanonical",
        "Available substances must be distinct ids in ascending order.",
        field: "available");

    public static readonly Error BenchSize = Error.Validation(
        "Release.BenchSize",
        $"Matching takes 1 to {MaximumBench} substances.",
        field: "available")
        .WithArgs(("max", MaximumBench));

    public static readonly Error StructureKeyMalformed = Error.Validation(
        "Release.StructureKeyMalformed",
        "A structure key is 32 lowercase hexadecimal characters.",
        field: "key");

    public static readonly Error CursorNotOffset = Error.Validation(
        "Release.CursorNotOffset",
        "The pagination cursor is not valid.",
        field: "cursor");

    public static readonly Error NotHeld = Error.Unexpected(
        "Release.NotHeld",
        "This instance does not hold the requested release.");

    public static Error TooDeep(int maximum) =>
        Error.Validation("Release.TooDeep", "Results are limited. Narrow the term rather than paging further.", field: "cursor")
            .WithArgs(("max", maximum));
}

using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Features.Substances;

public static class SubstanceErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Substance.NotFound", "No substance exists with the requested id.")
            .WithArgs(("id", id));

    public static Error SearchTooDeep(int maximum) =>
        Error.Validation(
            "Substance.SearchTooDeep",
            "Search results are limited. Narrow the term rather than paging further",
            field: "Cursor")
            .WithArgs(("max", maximum));
}

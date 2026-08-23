using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Features.Elements;

public static class ElementErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Element.NotFound", "No element exists with the requested id")
            .WithArgs(("id", id));

    public static Error SymbolNotFound(string symbol) =>
        Error.NotFound("Element.SymbolNotFound", "No element has that symbol.")
            .WithArgs(("symbol", symbol));
}

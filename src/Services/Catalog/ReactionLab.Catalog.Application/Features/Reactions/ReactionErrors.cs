using ReactionLab.SharedKernel;

namespace ReactionLab.Catalog.Application.Features.Reactions;

public static class ReactionErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Reaction.NotFound", "No reaction exists with the requested id.")
            .WithArgs(("id", id));
}

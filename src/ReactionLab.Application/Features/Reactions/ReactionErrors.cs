using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Features.Reactions;

public static class ReactionErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Reaction.NotFound", "No reaction exists with the requested id.")
            .WithArgs(("id", id));
}

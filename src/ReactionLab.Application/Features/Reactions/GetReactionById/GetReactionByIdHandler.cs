using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Features.Reactions.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Reactions;

namespace ReactionLab.Application.Features.Reactions.GetReactionById;

public sealed class GetReactionByIdHandler(IAppDbContext context)
    : IQueryHandler<GetReactionByIdQuery, ReactionResponse>
{
    public async ValueTask<Result<ReactionResponse>> HandleAsync(GetReactionByIdQuery query, CancellationToken cancellationToken)
    {
        var id = ReactionId.From(query.Id);

        var response = await ReactionQueries.FirstResponseAsync(
            context.Reactions.AsNoTracking().Where(reaction => reaction.Id == id),
            context.Substances.AsNoTracking(),
            query.Locale,
            cancellationToken);

        return response is null ? ReactionErrors.NotFound(query.Id) : response;
    }
}

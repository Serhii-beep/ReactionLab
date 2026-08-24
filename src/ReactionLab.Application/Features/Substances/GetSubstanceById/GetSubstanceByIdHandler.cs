using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Features.Substances.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Application.Features.Substances.GetSubstanceById;

public sealed class GetSubstanceByIdHandler(IAppDbContext context)
    : IQueryHandler<GetSubstanceByIdQuery, SubstanceResponse>
{
    public async ValueTask<Result<SubstanceResponse>> HandleAsync(GetSubstanceByIdQuery query, CancellationToken cancellationToken)
    {
        var id = SubstanceId.From(query.Id);

        var response = await SubstanceQueries.FirstResponseAsync(
            context.Substances.AsNoTracking().Where(substance => substance.Id == id),
            query.Locale,
            cancellationToken);

        return response is null ? SubstanceErrors.NotFound(query.Id) : response;
    }
}

using Microsoft.EntityFrameworkCore;
using ReactionLab.Application.Common.Abstractions;
using ReactionLab.Application.Features.Elements.Contracts;
using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Application.Features.Elements.GetElementById;

public sealed class GetElementByIdHandler(IAppDbContext context)
    : IQueryHandler<GetElementByIdQuery, ElementResponse>
{
    public async ValueTask<Result<ElementResponse>> HandleAsync(
        GetElementByIdQuery query,
        CancellationToken cancellationToken)
    {
        var id = ElementId.From(query.Id);

        var response = await ElementQueries.FirstResponseAsync(
            context.Elements.AsNoTracking().Where(element => element.Id == id),
            query.Locale,
            cancellationToken);

        return response is null ? ElementErrors.NotFound(query.Id) : response;
    }
}

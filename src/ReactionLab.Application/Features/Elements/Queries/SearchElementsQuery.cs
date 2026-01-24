using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Elements.Queries;

public record SearchElementsQuery(string SearchTerm) : IRequest<IReadOnlyList<ElementSummaryDto>>;

public class SearchElementsQueryHandler : IRequestHandler<SearchElementsQuery, IReadOnlyList<ElementSummaryDto>>
{
    private readonly IElementRepository _elementRepository;
    private readonly IMapper _mapper;

    public SearchElementsQueryHandler(IElementRepository elementRepository, IMapper mapper)
    {
        _elementRepository = elementRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ElementSummaryDto>> Handle(SearchElementsQuery request, CancellationToken cancellationToken)
    {
        var elements = await _elementRepository.SearchByNameAsync(request.SearchTerm, cancellationToken);
        return _mapper.Map<IReadOnlyList<ElementSummaryDto>>(elements);
    }
}
using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Elements.Queries;

public record GetAllElementsQuery : IRequest<IReadOnlyList<ElementSummaryDto>>;

public class GetAllElementsQueryHandler : IRequestHandler<GetAllElementsQuery, IReadOnlyList<ElementSummaryDto>>
{
    private readonly IElementRepository _elementRepository;
    private readonly IMapper _mapper;

    public GetAllElementsQueryHandler(IElementRepository elementRepository, IMapper mapper)
    {
        _elementRepository = elementRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ElementSummaryDto>> Handle(GetAllElementsQuery request, CancellationToken cancellationToken)
    {
        var elements = await _elementRepository.GetAllAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<ElementSummaryDto>>(elements);
    }
}

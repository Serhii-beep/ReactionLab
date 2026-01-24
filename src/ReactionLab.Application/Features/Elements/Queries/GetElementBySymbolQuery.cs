using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Elements.Queries;

public record GetElementBySymbolQuery(string Symbol) : IRequest<ElementDto?>;

public class GetElementBySymbolQueryHandler : IRequestHandler<GetElementBySymbolQuery, ElementDto?>
{
    private readonly IElementRepository _elementRepository;
    private readonly IMapper _mapper;

    public GetElementBySymbolQueryHandler(IElementRepository elementRepository, IMapper mapper)
    {
        _elementRepository = elementRepository;
        _mapper = mapper;
    }

    public async Task<ElementDto?> Handle(GetElementBySymbolQuery request, CancellationToken cancellationToken)
    {
        var element = await _elementRepository.GetBySymbolAsync(request.Symbol, cancellationToken);
        return element is null ? null : _mapper.Map<ElementDto>(element);
    }
}
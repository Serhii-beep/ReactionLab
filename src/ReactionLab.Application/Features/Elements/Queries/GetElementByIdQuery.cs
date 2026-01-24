using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Elements.Queries;

public record GetElementByIdQuery(Guid Id) : IRequest<ElementDto?>;

public class GetElementByIdQueryHandler : IRequestHandler<GetElementByIdQuery, ElementDto?>
{
    private readonly IElementRepository _elementRepository;
    private readonly IMapper _mapper;

    public GetElementByIdQueryHandler(IElementRepository elementRepository, IMapper mapper)
    {
        _elementRepository = elementRepository;
        _mapper = mapper;
    }

    public async Task<ElementDto?> Handle(GetElementByIdQuery request, CancellationToken cancellationToken)
    {
        var element = await _elementRepository.GetByIdAsync(request.Id, cancellationToken);
        return element is null ? null : _mapper.Map<ElementDto>(element);
    }
}
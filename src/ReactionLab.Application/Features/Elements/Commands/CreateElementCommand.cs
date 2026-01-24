using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Elements.Commands;

public record CreateElementCommand(CreateElementDto Element) : IRequest<ElementDto>;

public class CreateElementCommandHandler : IRequestHandler<CreateElementCommand, ElementDto>
{
    private readonly IElementRepository _elementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateElementCommandHandler(IElementRepository elementRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _elementRepository = elementRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ElementDto> Handle(CreateElementCommand request, CancellationToken cancellationToken)
    {
        var element = _mapper.Map<Element>(request.Element);
        await _elementRepository.AddAsync(element, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ElementDto>(element);
    }
}
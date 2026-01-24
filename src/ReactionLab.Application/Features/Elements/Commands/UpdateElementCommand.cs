using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Elements.Commands;

public record UpdateElementCommand(Guid Id, UpdateElementDto Element) : IRequest<ElementDto?>;

public class UpdateElementCommandHandler : IRequestHandler<UpdateElementCommand, ElementDto?>
{
    private readonly IElementRepository _elementRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateElementCommandHandler(IElementRepository elementRepository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _elementRepository = elementRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ElementDto?> Handle(UpdateElementCommand request, CancellationToken cancellationToken)
    {
        var element = await _elementRepository.GetByIdAsync(request.Id, cancellationToken);

        if (element is null)
        {
            return null;
        }

        _mapper.Map(request.Element, element);
        _elementRepository.Update(element);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ElementDto>(element);
    }
}
using MediatR;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Elements.Commands;

public record DeleteElementCommand(Guid Id) : IRequest<bool>;

public class DeleteElementCommandHandler : IRequestHandler<DeleteElementCommand, bool>
{
    private readonly IElementRepository _elementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteElementCommandHandler(
        IElementRepository elementRepository,
        IUnitOfWork unitOfWork)
    {
        _elementRepository = elementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteElementCommand request, CancellationToken cancellationToken)
    {
        var element = await _elementRepository.GetByIdAsync(request.Id, cancellationToken);

        if (element is null)
        {
            return false;
        }

        _elementRepository.Remove(element);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
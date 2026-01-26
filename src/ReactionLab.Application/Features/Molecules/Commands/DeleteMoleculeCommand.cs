using MediatR;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Commands;

public record DeleteMoleculeCommand(Guid Id) : IRequest<bool>;

public class DeleteMoleculeCommandHandler : IRequestHandler<DeleteMoleculeCommand, bool>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMoleculeCommandHandler(
        IMoleculeRepository moleculeRepository,
        IUnitOfWork unitOfWork)
    {
        _moleculeRepository = moleculeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteMoleculeCommand request, CancellationToken cancellationToken)
    {
        var molecule = await _moleculeRepository.GetByIdAsync(request.Id, cancellationToken);

        if (molecule is null)
        {
            return false;
        }

        _moleculeRepository.Remove(molecule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Commands;

public record UpdateMoleculeCommand(Guid Id, UpdateMoleculeDto Molecule) : IRequest<MoleculeDto?>;

public class UpdateMoleculeCommandHandler : IRequestHandler<UpdateMoleculeCommand, MoleculeDto?>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateMoleculeCommandHandler(
        IMoleculeRepository moleculeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _moleculeRepository = moleculeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<MoleculeDto?> Handle(UpdateMoleculeCommand request, CancellationToken cancellationToken)
    {
        var molecule = await _moleculeRepository.GetWithElementsAsync(request.Id, cancellationToken);

        if (molecule is null)
        {
            return null;
        }

        _mapper.Map(request.Molecule, molecule);
        _moleculeRepository.Update(molecule);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<MoleculeDto>(molecule);
    }
}

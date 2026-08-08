using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Commands;

public record CreateMoleculeCommand(CreateMoleculeDto Molecule) : IRequest<MoleculeDto>;

public class CreateMoleculeCommandHandler : IRequestHandler<CreateMoleculeCommand, MoleculeDto>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateMoleculeCommandHandler(
        IMoleculeRepository moleculeRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _moleculeRepository = moleculeRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<MoleculeDto> Handle(CreateMoleculeCommand request, CancellationToken cancellationToken)
    {
        var molecule = _mapper.Map<Molecule>(request.Molecule);

        if (request.Molecule.Elements is { Count: > 0 })
        {
            foreach (var elementDto in request.Molecule.Elements)
            {
                molecule.MoleculeElements.Add(new MoleculeElement
                {
                    ElementId = elementDto.ElementId,
                    Count = elementDto.Count
                });
            }
        }

        await _moleculeRepository.AddAsync(molecule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdMolecule = await _moleculeRepository.GetWithElementsAsync(molecule.Id, cancellationToken);
        return _mapper.Map<MoleculeDto>(createdMolecule);
    }
}

using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Queries;

public record GetMoleculeByIdQuery(Guid Id) : IRequest<MoleculeDto?>;

public class GetMoleculeByIdQueryHandler : IRequestHandler<GetMoleculeByIdQuery, MoleculeDto?>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IMapper _mapper;

    public GetMoleculeByIdQueryHandler(IMoleculeRepository moleculeRepository, IMapper mapper)
    {
        _moleculeRepository = moleculeRepository;
        _mapper = mapper;
    }

    public async Task<MoleculeDto?> Handle(GetMoleculeByIdQuery request, CancellationToken cancellationToken)
    {
        var molecule = await _moleculeRepository.GetWithElementsAsync(request.Id, cancellationToken);
        return molecule is null ? null : _mapper.Map<MoleculeDto>(molecule);
    }
}
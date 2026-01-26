using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Queries;

public record GetAllMoleculesQuery : IRequest<IReadOnlyList<MoleculeSummaryDto>>;

public class GetAllMoleculesQueryHandler : IRequestHandler<GetAllMoleculesQuery, IReadOnlyList<MoleculeSummaryDto>>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IMapper _mapper;

    public GetAllMoleculesQueryHandler(IMoleculeRepository moleculeRepository, IMapper mapper)
    {
        _moleculeRepository = moleculeRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<MoleculeSummaryDto>> Handle(GetAllMoleculesQuery request, CancellationToken cancellationToken)
    {
        var molecules = await _moleculeRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<MoleculeSummaryDto>>(molecules);
    }
}
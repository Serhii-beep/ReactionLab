using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Queries;

public record SearchMoleculesQuery(string SearchTerm) : IRequest<IReadOnlyList<MoleculeSummaryDto>>;

public class SearchMoleculesQueryHandler : IRequestHandler<SearchMoleculesQuery, IReadOnlyList<MoleculeSummaryDto>>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IMapper _mapper;

    public SearchMoleculesQueryHandler(IMoleculeRepository moleculeRepository, IMapper mapper)
    {
        _moleculeRepository = moleculeRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<MoleculeSummaryDto>> Handle(SearchMoleculesQuery request, CancellationToken cancellationToken)
    {
        var molecules = await _moleculeRepository.SearchByNameAsync(request.SearchTerm, cancellationToken);
        return _mapper.Map<IReadOnlyList<MoleculeSummaryDto>>(molecules);
    }
}
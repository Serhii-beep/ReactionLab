using AutoMapper;
using MediatR;
using ReactionLab.Application.Common;
using ReactionLab.Application.DTOs;
using ReactionLab.Application.Interfaces;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Queries;

public record GetAllMoleculesQuery : IRequest<IReadOnlyList<MoleculeSummaryDto>>;

public class GetAllMoleculesQueryHandler : IRequestHandler<GetAllMoleculesQuery, IReadOnlyList<MoleculeSummaryDto>>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetAllMoleculesQueryHandler(IMoleculeRepository moleculeRepository, IMapper mapper, ICacheService cacheService)
    {
        _moleculeRepository = moleculeRepository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<IReadOnlyList<MoleculeSummaryDto>> Handle(GetAllMoleculesQuery request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrSetAsync(
            CacheKeys.Molecules.All,
            async () =>
            {
                var molecules = await _moleculeRepository.GetAllAsync(cancellationToken);
                return _mapper.Map<IReadOnlyList<MoleculeSummaryDto>>(molecules);
            },
            TimeSpan.FromHours(1),
            cancellationToken);
    }
}
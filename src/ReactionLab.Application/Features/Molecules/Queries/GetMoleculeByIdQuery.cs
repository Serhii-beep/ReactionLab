using AutoMapper;
using MediatR;
using ReactionLab.Application.Common;
using ReactionLab.Application.DTOs;
using ReactionLab.Application.Interfaces;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Queries;

public record GetMoleculeByIdQuery(Guid Id) : IRequest<MoleculeDto?>;

public class GetMoleculeByIdQueryHandler : IRequestHandler<GetMoleculeByIdQuery, MoleculeDto?>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetMoleculeByIdQueryHandler(IMoleculeRepository moleculeRepository, IMapper mapper, ICacheService cacheService)
    {
        _moleculeRepository = moleculeRepository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<MoleculeDto?> Handle(GetMoleculeByIdQuery request, CancellationToken cancellationToken)
    {
        return await _cacheService.GetOrSetAsync(
            CacheKeys.Molecules.ById(request.Id),
            async () =>
            {
                var molecule = await _moleculeRepository.GetWithElementsAsync(request.Id, cancellationToken);
                return molecule is null ? null : _mapper.Map<MoleculeDto>(molecule);
            },
            TimeSpan.FromHours(1),
            cancellationToken);
    }
}
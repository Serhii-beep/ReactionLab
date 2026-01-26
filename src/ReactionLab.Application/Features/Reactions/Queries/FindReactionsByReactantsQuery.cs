using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Queries;

public record FindReactionsByReactantsQuery(
    IEnumerable<Guid> ElementIds,
    IEnumerable<Guid> MoleculeIds
) : IRequest<IReadOnlyList<ReactionSummaryDto>>;

public class FindReactionsByReactantsQueryHandler : IRequestHandler<FindReactionsByReactantsQuery, IReadOnlyList<ReactionSummaryDto>>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IMapper _mapper;

    public FindReactionsByReactantsQueryHandler(IReactionRepository reactionRepository, IMapper mapper)
    {
        _reactionRepository = reactionRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ReactionSummaryDto>> Handle(FindReactionsByReactantsQuery request, CancellationToken cancellationToken)
    {
        var reactions = await _reactionRepository.FindByReactantsAsync(
            request.ElementIds,
            request.MoleculeIds,
            cancellationToken);
        return _mapper.Map<IReadOnlyList<ReactionSummaryDto>>(reactions);
    }
}
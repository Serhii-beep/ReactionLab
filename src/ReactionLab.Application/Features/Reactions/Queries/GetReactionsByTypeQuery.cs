using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Queries;

public record GetReactionsByTypeQuery(ReactionType Type) : IRequest<IReadOnlyList<ReactionSummaryDto>>;

public class GetReactionsByTypeQueryHandler : IRequestHandler<GetReactionsByTypeQuery, IReadOnlyList<ReactionSummaryDto>>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IMapper _mapper;

    public GetReactionsByTypeQueryHandler(IReactionRepository reactionRepository, IMapper mapper)
    {
        _reactionRepository = reactionRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ReactionSummaryDto>> Handle(GetReactionsByTypeQuery request, CancellationToken cancellationToken)
    {
        var reactions = await _reactionRepository.GetByTypeAsync(request.Type, cancellationToken);
        return _mapper.Map<IReadOnlyList<ReactionSummaryDto>>(reactions);
    }
}
using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Queries;

public record GetAllReactionsQuery : IRequest<IReadOnlyList<ReactionSummaryDto>>;

public class GetAllReactionsQueryHandler : IRequestHandler<GetAllReactionsQuery, IReadOnlyList<ReactionSummaryDto>>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IMapper _mapper;

    public GetAllReactionsQueryHandler(IReactionRepository reactionRepository, IMapper mapper)
    {
        _reactionRepository = reactionRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ReactionSummaryDto>> Handle(GetAllReactionsQuery request, CancellationToken cancellationToken)
    {
        var reactions = await _reactionRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<ReactionSummaryDto>>(reactions);
    }
}
using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Queries;

public record SearchReactionsQuery(string SearchTerm) : IRequest<IReadOnlyList<ReactionSummaryDto>>;

public class SearchReactionsQueryHandler : IRequestHandler<SearchReactionsQuery, IReadOnlyList<ReactionSummaryDto>>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IMapper _mapper;

    public SearchReactionsQueryHandler(IReactionRepository reactionRepository, IMapper mapper)
    {
        _reactionRepository = reactionRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<ReactionSummaryDto>> Handle(SearchReactionsQuery request, CancellationToken cancellationToken)
    {
        var reactions = await _reactionRepository.SearchByNameAsync(request.SearchTerm, cancellationToken);
        return _mapper.Map<IReadOnlyList<ReactionSummaryDto>>(reactions);
    }
}
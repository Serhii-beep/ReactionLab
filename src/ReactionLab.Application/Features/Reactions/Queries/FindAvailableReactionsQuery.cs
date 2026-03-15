using AutoMapper;
using MediatR;
using ReactionLab.Application.Common;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.DTOs;
using ReactionLab.Application.Interfaces;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Queries;

public record FindAvailableReactionsQuery(
    IEnumerable<Guid> MoleculeIds,
    IEnumerable<Guid> ElementIds,
    string? SearchTerm = null,
    int PageSize = 20,
    string? Cursor = null
) : IRequest<CursorPagedResult<ReactionSummaryDto>>;

public class FindAvailableReactionsQueryHandler : IRequestHandler<FindAvailableReactionsQuery, CursorPagedResult<ReactionSummaryDto>>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public FindAvailableReactionsQueryHandler(
        IReactionRepository reactionRepository,
        IMapper mapper,
        ICacheService cacheService)
    {
        _reactionRepository = reactionRepository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<CursorPagedResult<ReactionSummaryDto>> Handle(FindAvailableReactionsQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var cursor = Cursor.Decode(request.Cursor);

        var cacheKey = CacheKeys.Reactions.Available(
            request.MoleculeIds,
            request.ElementIds,
            request.SearchTerm,
            cursor?.CreatedAt.Ticks ?? 0,
            pageSize);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var (items, hasMore) = await _reactionRepository.FindAvailableReactionsAsync(
                    request.MoleculeIds,
                    request.ElementIds,
                    request.SearchTerm,
                    pageSize,
                    cursor?.CreatedAt,
                    cursor?.Id,
                    cancellationToken);

                string? nextCursor = null;
                if (hasMore && items.Count > 0)
                {
                    var lastItem = items[^1];
                    nextCursor = new Cursor
                    {
                        CreatedAt = lastItem.CreatedAt,
                        Id = lastItem.Id
                    }.Encode();
                }

                return new CursorPagedResult<ReactionSummaryDto>
                {
                    Items = _mapper.Map<IReadOnlyList<ReactionSummaryDto>>(items),
                    NextCursor = nextCursor,
                    HasMore = hasMore,
                    PageSize = pageSize
                };
            },
            TimeSpan.FromMinutes(10),
            cancellationToken);
    }
}

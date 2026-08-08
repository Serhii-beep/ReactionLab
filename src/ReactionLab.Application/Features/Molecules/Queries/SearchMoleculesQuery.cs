using AutoMapper;
using MediatR;
using ReactionLab.Application.Common;
using ReactionLab.Application.Common.Pagination;
using ReactionLab.Application.DTOs;
using ReactionLab.Application.Interfaces;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Molecules.Queries;

public record SearchMoleculesQuery(string? SearchTerm, int PageSize = 20, string? Cursor = null) : IRequest<CursorPagedResult<MoleculeSummaryDto>>;

public class SearchMoleculesQueryHandler : IRequestHandler<SearchMoleculesQuery, CursorPagedResult<MoleculeSummaryDto>>
{
    private readonly IMoleculeRepository _moleculeRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public SearchMoleculesQueryHandler(IMoleculeRepository moleculeRepository, IMapper mapper, ICacheService cacheService)
    {
        _moleculeRepository = moleculeRepository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<CursorPagedResult<MoleculeSummaryDto>> Handle(SearchMoleculesQuery request, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var cursor = Cursor.Decode(request.Cursor);

        var cacheKey = CacheKeys.Molecules.Search(
            request.SearchTerm ?? "all",
            cursor?.CreatedAt.Ticks ?? 0,
            pageSize);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                var (items, _) = await _moleculeRepository.SearchAsync(request.SearchTerm, pageSize, cursor?.CreatedAt, cursor?.Id, cancellationToken);

                var hasMore = items.Count > pageSize;
                var resultItems = hasMore ? items.Take(pageSize).ToList() : items;

                string? nextCursor = null;
                if (hasMore && resultItems.Count > 0)
                {
                    var lastItem = resultItems[^1];
                    nextCursor = new Cursor
                    {
                        CreatedAt = lastItem.CreatedAt,
                        Id = lastItem.Id
                    }.Encode();
                }

                return new CursorPagedResult<MoleculeSummaryDto>
                {
                    Items = _mapper.Map<IReadOnlyList<MoleculeSummaryDto>>(resultItems),
                    NextCursor = nextCursor,
                    HasMore = hasMore,
                    PageSize = pageSize
                };
            }, TimeSpan.FromMinutes(5), cancellationToken);
    }
}

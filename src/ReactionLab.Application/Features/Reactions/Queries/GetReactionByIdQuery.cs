using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Queries;

public record GetReactionByIdQuery(Guid Id) : IRequest<ReactionDto?>;

public class GetReactionByIdQueryHandler : IRequestHandler<GetReactionByIdQuery, ReactionDto?>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IMapper _mapper;

    public GetReactionByIdQueryHandler(IReactionRepository reactionRepository, IMapper mapper)
    {
        _reactionRepository = reactionRepository;
        _mapper = mapper;
    }

    public async Task<ReactionDto?> Handle(GetReactionByIdQuery request, CancellationToken cancellationToken)
    {
        var reaction = await _reactionRepository.GetWithParticipantsAsync(request.Id, cancellationToken);
        return reaction is null ? null : _mapper.Map<ReactionDto>(reaction);
    }
}
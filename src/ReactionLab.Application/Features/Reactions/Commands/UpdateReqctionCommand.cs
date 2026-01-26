using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Commands;

public record UpdateReactionCommand(Guid Id, UpdateReactionDto Reaction) : IRequest<ReactionDto?>;

public class UpdateReactionCommandHandler : IRequestHandler<UpdateReactionCommand, ReactionDto?>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateReactionCommandHandler(
        IReactionRepository reactionRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ReactionDto?> Handle(UpdateReactionCommand request, CancellationToken cancellationToken)
    {
        var reaction = await _reactionRepository.GetWithParticipantsAsync(request.Id, cancellationToken);

        if (reaction is null)
        {
            return null;
        }

        _mapper.Map(request.Reaction, reaction);
        _reactionRepository.Update(reaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ReactionDto>(reaction);
    }
}
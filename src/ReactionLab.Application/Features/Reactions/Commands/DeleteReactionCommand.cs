using MediatR;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Commands;

public record DeleteReactionCommand(Guid Id) : IRequest<bool>;

public class DeleteReactionCommandHandler : IRequestHandler<DeleteReactionCommand, bool>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteReactionCommandHandler(
        IReactionRepository reactionRepository,
        IUnitOfWork unitOfWork)
    {
        _reactionRepository = reactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteReactionCommand request, CancellationToken cancellationToken)
    {
        var reaction = await _reactionRepository.GetByIdAsync(request.Id, cancellationToken);

        if (reaction is null)
        {
            return false;
        }

        _reactionRepository.Remove(reaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}

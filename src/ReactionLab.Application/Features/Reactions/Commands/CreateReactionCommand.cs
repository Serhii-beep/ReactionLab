using AutoMapper;
using MediatR;
using ReactionLab.Application.DTOs;
using ReactionLab.Domain.Entities;
using ReactionLab.Domain.Interfaces;

namespace ReactionLab.Application.Features.Reactions.Commands;

public record CreateReactionCommand(CreateReactionDto Reaction) : IRequest<ReactionDto>;

public class CreateReactionCommandHandler : IRequestHandler<CreateReactionCommand, ReactionDto>
{
    private readonly IReactionRepository _reactionRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateReactionCommandHandler(
        IReactionRepository reactionRepository,
        IRepository<Tag> tagRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _reactionRepository = reactionRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ReactionDto> Handle(CreateReactionCommand request, CancellationToken cancellationToken)
    {
        var reaction = _mapper.Map<Reaction>(request.Reaction);

        if (request.Reaction.Participants is { Count: > 0 })
        {
            foreach (var participantDto in request.Reaction.Participants)
            {
                reaction.Participants.Add(new ReactionParticipant
                {
                    ElementId = participantDto.ElementId,
                    MoleculeId = participantDto.MoleculeId,
                    Role = participantDto.Role,
                    Coefficient = participantDto.Coefficient,
                    State = participantDto.State
                });
            }
        }

        if (request.Reaction.Tags is { Count: > 0 })
        {
            var existingTags = await _tagRepository.GetAllAsync(cancellationToken);

            foreach (var tagName in request.Reaction.Tags)
            {
                var tag = existingTags.FirstOrDefault(t =>
                    t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                if (tag is null)
                {
                    tag = new Tag { Name = tagName };
                    await _tagRepository.AddAsync(tag, cancellationToken);
                }

                reaction.ReactionTags.Add(new ReactionTag
                {
                    Tag = tag
                });
            }
        }

        await _reactionRepository.AddAsync(reaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var createdReaction = await _reactionRepository.GetWithParticipantsAsync(reaction.Id, cancellationToken);
        return _mapper.Map<ReactionDto>(createdReaction);
    }
}

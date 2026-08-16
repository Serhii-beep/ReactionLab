using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Domain.Reactions;

public sealed class ReactionParticipant : Entity<ReactionParticipantId>
{
    internal ReactionParticipant(
        ReactionParticipantId id,
        SubstanceId substanceId,
        ParticipantRole role,
        int coefficient,
        MatterState? state): base(id)
    {
        SubstanceId = substanceId;
        Role = role;
        Coefficient = coefficient;
        State = state;
    }

    private ReactionParticipant()
    {

    }

    public SubstanceId SubstanceId { get; private set; }

    public ParticipantRole Role { get; private set; }

    public int Coefficient { get; private set; }

    public MatterState? State { get; private set; }
}

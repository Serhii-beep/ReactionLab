using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Domain.Entities;

public class ReactionParticipant : BaseEntity
{
    public Guid ReactionId { get; set; }

    public Guid? MoleculeId { get; set; }

    public Guid? ElementId { get; set; }

    public ParticipantRole Role { get; set; }

    public int Coefficient { get; set; } = 1;

    public MatterState? State { get; set; }

    public Reaction Reaction { get; set; } = null!;

    public Molecule? Molecule { get; set; }

    public Element? Element { get; set; }
}
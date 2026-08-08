using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Entities;

public class MoleculeElement : BaseEntity
{
    public Guid MoleculeId { get; set; }

    public Guid ElementId { get; set; }

    public int Count { get; set; }

    public Molecule Molecule { get; set; } = null!;

    public Element Element { get; set; } = null!;
}

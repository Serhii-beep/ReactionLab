using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Domain.Entities;

public class Bond : BaseEntity
{
    public Guid MoleculeId { get; set; }

    public int Atom1Index { get; set; }

    public int Atom2Index { get; set; }

    public BondType BondType { get; set; }

    public decimal? BondLength { get; set; }

    public decimal? BondEnergy { get; set; }

    public Molecule Molecule { get; set; } = null!;
}

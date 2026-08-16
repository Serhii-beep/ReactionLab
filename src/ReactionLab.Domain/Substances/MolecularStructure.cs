using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;

namespace ReactionLab.Domain.Substances;

public sealed class MolecularStructure
{
    public static readonly Error NoAtoms = Error.Validation(
        "MolecularStructure.NoAtoms",
        "A structure must contain at least one atom.");

    public static readonly Error BondIndexOutOfRange = Error.Validation(
        "MolecularStructure.BondIndexOutOfRange",
        "A bond references an atom index that does not exist.");

    public static readonly Error SelfBond = Error.Validation(
        "MolecularStructure.SelfBond",
        "A bond cannot join an atom to itself.");

    public static readonly Error DuplicateBond = Error.Validation(
        "MolecularStructure.DuplicatedBond",
        "The same pair of atoms is bonded more than once.");

    private readonly List<AtomPlacement> _atoms;
    private readonly List<Bond> _bonds;

    private MolecularStructure(List<AtomPlacement> atoms, List<Bond> bonds)
    {
        _atoms = atoms;
        _bonds = bonds;
    }

    public IReadOnlyList<AtomPlacement> Atoms => _atoms.AsReadOnly();

    public IReadOnlyList<Bond> Bonds => _bonds.AsReadOnly();

    public bool HasExplicitHydrogens => _atoms.Any(atom => atom.Symbol.Value == "H");

    public static Result<MolecularStructure> Create(
        IEnumerable<AtomPlacement>? atoms,
        IEnumerable<Bond>? bonds)
    {
        var atomList = atoms?.ToList() ?? [];
        var bondList = bonds?.ToList() ?? [];

        if (atomList.Count == 0)
        {
            return NoAtoms;
        }

        var seen = new HashSet<(int Low, int High)>();

        foreach (var bond in bondList)
        {
            if (bond.FromAtomIndex < 0 || bond.FromAtomIndex >= atomList.Count
                || bond.ToAtomIndex < 0 || bond.ToAtomIndex >= atomList.Count)
            {
                return BondIndexOutOfRange;
            }

            if (bond.FromAtomIndex == bond.ToAtomIndex)
            {
                return SelfBond;
            }

            var pair = bond.FromAtomIndex < bond.ToAtomIndex
                ? (bond.FromAtomIndex, bond.ToAtomIndex)
                : (bond.ToAtomIndex, bond.FromAtomIndex);

            if (!seen.Add(pair))
            {
                return DuplicateBond;
            }
        }

        return new MolecularStructure(atomList, bondList);
    }

    public IReadOnlyDictionary<ElementSymbol, int> Composition() =>
        _atoms.GroupBy(atom => atom.Symbol).ToDictionary(group => group.Key, group => group.Count());
}

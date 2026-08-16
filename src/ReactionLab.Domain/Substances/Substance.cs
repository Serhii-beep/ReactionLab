using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Substances.Events;

namespace ReactionLab.Domain.Substances;

public sealed class Substance : AggregateRoot<SubstanceId>
{
    public const int MaximumNameLength = 200;
    public const int MaximumIupacNameLength = 300;
    public const int MaximumCategoryLength = 100;

    public static readonly Error NameRequired = Error.Validation(
        "Substance.NameRequired",
        "Substance name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "Substance.NameTooLong",
        $"Substance name must not exceed {MaximumNameLength} characters.");

    public static readonly Error StructureCompositionMismatch = Error.Validation(
        "Substance.StructureCompositionMismatch",
        "The structure's atoms do not match the substance's formula.");

    public static readonly Error PartialHydrogens = Error.Validation(
        "Substance.PartialHydrogens",
        "A structure must place either all of the formula's hydrogens or none of them.");

    private readonly List<string> _commonNames = [];
    private readonly List<string> _uses = [];
    private readonly List<string> _interestingFacts = [];

    private Substance(
        SubstanceId id,
        ChemicalFormula formula,
        string name,
        SubstanceKind kind,
        bool isOrganic,
        MatterState stateAtRoomTemperature) : base(id)
    {
        Formula = formula;
        Name = name;
        Kind = kind;
        IsOrganic = isOrganic;
        StateAtRoomTemperature = stateAtRoomTemperature;
    }

    private Substance()
    {

    }

    public ChemicalFormula Formula { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public SubstanceKind Kind { get; private set; }

    public bool IsOrganic { get; private set; }

    public MatterState StateAtRoomTemperature { get; private set; }

    public MolecularWeight? Weight { get; private set; }

    public MolecularStructure? Structure { get; private set; }

    public string? IupacName { get; private set; }

    public string? Category { get; private set; }

    public string? Description { get; private set; }

    public string? SafetyInformation { get; private set; }

    public IReadOnlyList<string> CommonNames => _commonNames.AsReadOnly();

    public IReadOnlyList<string> Uses => _uses.AsReadOnly();

    public IReadOnlyList<string> InterestingFacts => _interestingFacts.AsReadOnly();

    public static Result<Substance> Create(
        ChemicalFormula formula,
        string? name,
        SubstanceKind kind,
        bool isOrganic,
        MatterState stateAtRoomTemperature)
    {
        var validatedName = ValidateName(name);
        if (validatedName.IsFailure)
        {
            return validatedName.Error;
        }

        var substance = new Substance(
            SubstanceId.New(),
            formula,
            validatedName.Value,
            kind,
            isOrganic,
            stateAtRoomTemperature);

        substance.Raise(new SubstanceCreated(substance.Id, formula));

        return substance;
    }

    public Result DefineStructure(MolecularStructure structure)
    {
        var fromStructure = structure.Composition();
        var hydrogen = ElementSymbol.Create("H").Value;

        foreach (var (symbol, formulaCount) in Formula.Composition)
        {
            fromStructure.TryGetValue(symbol, out var structureCount);

            if (symbol == hydrogen)
            {
                if (structureCount != 0 && structureCount != formulaCount)
                {
                    return Result.Failure(PartialHydrogens);
                }

                continue;
            }

            if (structureCount != formulaCount)
            {
                return Result.Failure(StructureCompositionMismatch);
            }
        }

        var formulaSymbols = Formula.Composition.Select(quantity => quantity.Symbol).ToHashSet();
        if (fromStructure.Keys.Any(symbol => !formulaSymbols.Contains(symbol)))
        {
            return Result.Failure(StructureCompositionMismatch);
        }

        Structure = structure;

        return Result.Success();
    }

    public Result RecordWeight(MolecularWeight weight)
    {
        Weight = weight;

        return Result.Success();
    }

    public Result Describe(
        string? iupacName,
        IEnumerable<string>? commonNames,
        string? category,
        string? description)
    {
        IupacName = Clean(iupacName, MaximumIupacNameLength);
        Category = Clean(category, MaximumCategoryLength);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        Replace(_commonNames, commonNames);

        return Result.Success();
    }

    public Result DescribeSafety(
        string? safetyInformation,
        IEnumerable<string>? uses,
        IEnumerable<string>? interestingFacts)
    {
        SafetyInformation = string.IsNullOrWhiteSpace(safetyInformation)
            ? null
            : safetyInformation.Trim();

        Replace(_uses, uses);
        Replace(_interestingFacts, interestingFacts);

        return Result.Success();
    }

    private static Result<string> ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return NameRequired;
        }

        var trimmed = name.Trim();

        return trimmed.Length > MaximumNameLength ? NameTooLong : trimmed;
    }

    private static string? Clean(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maximumLength ? trimmed : trimmed[..maximumLength];
    }

    public static void Replace(List<string> target, IEnumerable<string>? values)
    {
        target.Clear();

        if (values is null)
        {
            return;
        }

        target.AddRange(values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()));
    }
}

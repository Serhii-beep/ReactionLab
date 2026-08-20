using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.Substances.Events;

namespace ReactionLab.Domain.Substances;

public sealed class Substance : AggregateRoot<SubstanceId>
{
    public const int MaximumCategoryLength = 100;

    public static readonly Error StructureCompositionMismatch = Error.Validation(
        "Substance.StructureCompositionMismatch",
        "The structure's atoms do not match the substance's formula.");

    public static readonly Error PartialHydrogens = Error.Validation(
        "Substance.PartialHydrogens",
        "A structure must place either all of the formula's hydrogens or none of them.");

    private Translations<SubstanceContent> _translations = null!;

    private Substance(
        SubstanceId id,
        ChemicalFormula formula,
        Translations<SubstanceContent> translations,
        SubstanceKind kind,
        bool isOrganic,
        MatterState stateAtRoomTemperature) : base(id)
    {
        Formula = formula;
        _translations = translations;
        Kind = kind;
        IsOrganic = isOrganic;
        StateAtRoomTemperature = stateAtRoomTemperature;
    }

    private Substance()
    {

    }

    public ChemicalFormula Formula { get; private set; } = null!;

    public SubstanceKind Kind { get; private set; }

    public bool IsOrganic { get; private set; }

    public MatterState StateAtRoomTemperature { get; private set; }

    public MolecularWeight? Weight { get; private set; }

    public MolecularStructure? Structure { get; private set; }

    public string? Category { get; private set; }

    public Translations<SubstanceContent> Translations => _translations;

    public IReadOnlyList<SupportedLocale> Locales => _translations.Locales;

    public static Result<Substance> Create(
        ChemicalFormula formula,
        SubstanceContent content,
        SubstanceKind kind,
        bool isOrganic,
        MatterState stateAtRoomTemperature)
    {
        var translations = ReactionLab.Domain.Localization.Translations.Create(content);
        if (translations.IsFailure)
        {
            return translations.Error;
        }

        var substance = new Substance(
            SubstanceId.New(),
            formula,
            translations.Value,
            kind,
            isOrganic,
            stateAtRoomTemperature);

        substance.Raise(new SubstanceCreated(substance.Id, formula));

        return substance;
    }

    public SubstanceContent Content(SupportedLocale locale) => _translations.Resolve(locale);

    public Result Translate(SupportedLocale locale, SubstanceContent content)
    {
        _translations = _translations.With(locale, content);

        return Result.Success();
    }

    public Result Classify(string? category)
    {
        Category = TextNormalizer.Clean(category, MaximumCategoryLength);

        return Result.Success();
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
}

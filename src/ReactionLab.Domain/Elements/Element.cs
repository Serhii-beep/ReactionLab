using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements.Events;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Localization;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Domain.Elements;

public sealed class Element : AggregateRoot<ElementId>
{
    public const int MaximumElectronConfigurationLength = 100;

    public static readonly Error FBlockCannotHaveGroup = Error.Validation(
        "Element.FBlockCannotHaveGroup",
        "Lanthanides and actinides are not assigned a periodic group.");

    private Translations<ElementContent> _translations = null!;

    private Element(
        ElementId id,
        AtomicNumber atomicNumber,
        ElementSymbol symbol,
        Translations<ElementContent> translations,
        AtomicMass mass,
        ElementCategory category,
        PeriodicPosition position,
        MatterState stateAtRoomTemperature,
        HexColor displayColor) : base(id)
    {
        AtomicNumber = atomicNumber;
        Symbol = symbol;
        _translations = translations;
        Mass = mass;
        Category = category;
        Position = position;
        StateAtRoomTemperature = stateAtRoomTemperature;
        DisplayColor = displayColor;
    }

    private Element()
    {

    }

    public AtomicNumber AtomicNumber { get; private set; } = null!;

    public ElementSymbol Symbol { get; private set; } = null!;

    public AtomicMass Mass { get; private set; } = null!;

    public ElementCategory Category { get; private set; }

    public PeriodicPosition Position { get; private set; } = null!;

    public MatterState StateAtRoomTemperature { get; private set; }

    public HexColor DisplayColor { get; private set; } = null!;

    public Electronegativity? Electronegativity { get; private set; }

    public AtomicRadii? Radii { get; private set; }

    public Temperature? MeltingPoint { get; private set; }

    public Temperature? BoilingPoint { get; private set; }

    public string? ElectronConfiguration { get; private set; }

    public Translations<ElementContent> Translations => _translations;

    public IReadOnlyList<SupportedLocale> Locales => _translations.Locales;

    public static Result<Element> Create(
        AtomicNumber atomicNumber,
        ElementSymbol symbol,
        ElementContent content,
        AtomicMass mass,
        ElementCategory category,
        PeriodicPosition position,
        MatterState stateAtRoomTemperature,
        HexColor displayColor)
    {
        var translations = ReactionLab.Domain.Localization.Translations.Create(content);
        if (translations.IsFailure)
        {
            return translations.Error;
        }

        if (category is ElementCategory.Lanthanide or ElementCategory.Actinide && position.Group is not null)
        {
            return FBlockCannotHaveGroup;
        }

        var element = new Element(
            ElementId.New(),
            atomicNumber,
            symbol,
            translations.Value,
            mass,
            category,
            position,
            stateAtRoomTemperature,
            displayColor);

        element.Raise(new ElementCreated(element.Id, symbol));

        return element;
    }

    public ElementContent Content(SupportedLocale locale) => _translations.Resolve(locale);

    public Result Translate(SupportedLocale locale, ElementContent content)
    {
        _translations = _translations.With(locale, content);

        return Result.Success();
    }

    public Result DescribePhysicalProperties(
        Electronegativity? electronegativity,
        AtomicRadii? radii,
        Temperature? meltingPoint,
        Temperature? boilingPoint)
    {
        Electronegativity = electronegativity;
        Radii = radii;
        MeltingPoint = meltingPoint;
        BoilingPoint = boilingPoint;

        return Result.Success();
    }

    public Result RecordElectronConfiguration(string? electronConfiguration)
    {
        ElectronConfiguration = TextNormalizer.Clean(electronConfiguration, MaximumElectronConfigurationLength);

        return Result.Success();
    }

    public Result UpdateAppearance(HexColor displayColor, AtomicRadii? radii)
    {
        DisplayColor = displayColor;
        Radii = radii;

        return Result.Success();
    }
}

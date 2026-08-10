using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements.Events;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.SharedKernel;

namespace ReactionLab.Domain.Elements;

public sealed class Element : AggregateRoot<ElementId>
{
    public const int MaximumNameLength = 50;
    public const int MaximumElectronConfigurationLength = 100;

    public static readonly Error NameRequired = Error.Validation(
        "Element.NameRequired",
        "Element name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "Element.NameTooLong",
        $"Element name must not exceed {MaximumNameLength} characters.");

    public static readonly Error FBlockCannotHaveGroup = Error.Validation(
        "Element.FBlockCannotHaveGroup",
        "Lanthanides and actinides are not assigned a periodic group.");

    public static readonly Error BoilingPointBelowMeltingPoint = Error.Validation(
        "Element.BoilingPointBelowMeltingPoint",
        "Boiling point cannot be lower than melting point.");

    private readonly List<string> _interestingFacts = [];

    private Element(
        ElementId id,
        AtomicNumber atomicNumber,
        ElementSymbol symbol,
        string name,
        AtomicMass mass,
        ElementCategory category,
        PeriodicPosition position,
        MatterState stateAtRoomTemperature,
        HexColor displayColor) : base(id)
    {
        AtomicNumber = atomicNumber;
        Symbol = symbol;
        Name = name;
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

    public string Name { get; private set; } = null!;

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

    public string? DiscoveryInfo { get; private set; }

    public IReadOnlyList<string> InterestingFacts => _interestingFacts.AsReadOnly();

    public static Result<Element> Create(
        AtomicNumber atomicNumber,
        ElementSymbol symbol,
        string? name,
        AtomicMass mass,
        ElementCategory category,
        PeriodicPosition position,
        MatterState stateAtRoomTemperature,
        HexColor displayColor)
    {
        var nameResult = ValidateName(name);
        if (nameResult.IsFailure)
        {
            return nameResult.Error;
        }

        if (category is ElementCategory.Lanthanide or ElementCategory.Actinide && position.Group is not null)
        {
            return FBlockCannotHaveGroup;
        }

        var element = new Element(
            ElementId.New(),
            atomicNumber,
            symbol,
            nameResult.Value,
            mass,
            category,
            position,
            stateAtRoomTemperature,
            displayColor);

        element.Raise(new ElementCreated(element.Id, symbol));

        return element;
    }

    public Result DescribePhysicalProperties(
        Electronegativity? electronegativity,
        AtomicRadii? radii,
        Temperature? meltingPoint,
        Temperature? boilingPoint)
    {
        if (meltingPoint is not null && boilingPoint is not null
            && boilingPoint.Kelvin < meltingPoint.Kelvin)
        {
            return Result.Failure(BoilingPointBelowMeltingPoint);
        }

        Electronegativity = electronegativity;
        Radii = radii;
        MeltingPoint = meltingPoint;
        BoilingPoint = boilingPoint;

        return Result.Success();
    }

    public Result DescribeDiscovery(
        string? electronConfiguration,
        string? discoveryInfo,
        IEnumerable<string>? interestingFacts)
    {
        ElectronConfiguration = Truncate(electronConfiguration, MaximumElectronConfigurationLength);
        DiscoveryInfo = string.IsNullOrWhiteSpace(discoveryInfo) ? null : discoveryInfo.Trim();

        _interestingFacts.Clear();
        if (interestingFacts is not null)
        {
            _interestingFacts.AddRange(
                interestingFacts.Where(fact => !string.IsNullOrWhiteSpace(fact))
                    .Select(fact => fact.Trim()));
        }

        return Result.Success();
    }

    public Result UpdateAppearance(HexColor displayColor, AtomicRadii? radii)
    {
        DisplayColor = displayColor;
        Radii = radii;

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

    private static string? Truncate(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maximumLength ? trimmed : trimmed[..maximumLength];
    }
}

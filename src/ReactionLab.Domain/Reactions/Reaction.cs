using ReactionLab.Domain.Common;
using ReactionLab.Domain.Elements;
using ReactionLab.Domain.Enums;
using ReactionLab.Domain.Reactions.Events;
using ReactionLab.Domain.Substances;

namespace ReactionLab.Domain.Reactions;

public sealed class Reaction : AggregateRoot<ReactionId>
{
    public const int MaximumNameLength = 200;

    public static readonly Error NameRequired = Error.Validation(
        "Reaction.NameRequired",
        "Reaction name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "Reaction.NameTooLong",
        $"Reaction name must not exceed {MaximumNameLength} characters.");

    public static readonly Error NoReactants = Error.Validation(
        "Reaction.NoReactants",
        "A reaction must have at least one reactant.");

    public static readonly Error NoProducts = Error.Validation(
        "Reaction.NoProducts",
        "A reaction must have at least one product.");

    public static readonly Error NonPositiveCoefficient = Error.Validation(
        "Reaction.NonPositiveCoefficient",
        "Every stoichiometric coefficient must be at least 1.");

    public static readonly Error DuplicateParticipant = Error.Validation(
        "Reaction.DuplicateParticipant",
        "The same substance appears twice on the same side. Combine the coefficients instead.");

    public static readonly Error NotMassBalanced = Error.Validation(
        "Reaction.NotMassBalanced",
        "The reaction does not conserve mass. Reactant and product atom counts differ.");

    private readonly List<ReactionParticipant> _participants = [];
    private readonly List<string> _tags = [];
    private readonly List<string> _realWorldExamples = [];

    private Reaction(
        ReactionId id,
        string name,
        ReactionType type,
        DifficultyLevel difficulty,
        bool isReversible) : base(id)
    {
        Name = name;
        Type = type;
        Difficulty = difficulty;
        IsReversible = isReversible;
    }

    private Reaction()
    {

    }

    public string Name { get; private set; } = null!;

    public ReactionType Type { get; private set; }

    public DifficultyLevel Difficulty { get; private set; } = null!;

    public bool IsReversible { get; private set; }

    public Thermodynamics Energetics { get; private set; } = Thermodynamics.Unknown;

    public ReactionConditions Conditions { get; private set; } = ReactionConditions.Unspecified;

    public VisualizationHint Visualization { get; private set; } = VisualizationHint.None;

    public string? Description { get; private set; }

    public string? Mechanism { get; private set; }

    public string? SafetyWarnings { get; private set; }

    public IReadOnlyList<ReactionParticipant> Participants => _participants.AsReadOnly();

    public IReadOnlyList<string> Tags => _tags.AsReadOnly();

    public IReadOnlyList<string> RealWorldExamples => _realWorldExamples.AsReadOnly();

    public IEnumerable<ReactionParticipant> Reactants =>
        _participants.Where(participant => participant.Role == ParticipantRole.Reactant);

    public IEnumerable<ReactionParticipant> Products =>
        _participants.Where(participant => participant.Role == ParticipantRole.Product);

    public IReadOnlyList<SubstanceId> ReactantSignature =>
        Reactants.Select(participant => participant.SubstanceId)
            .Distinct()
            .OrderBy(id => id.Value)
            .ToList();

    public static Result<Reaction> Create(
        string? name,
        ReactionType type,
        IReadOnlyList<ParticipantSpecification> participants,
        DifficultyLevel difficulty,
        bool isReversible)
    {
        var validatedName = ValidateName(name);
        if (validatedName.IsFailure)
        {
            return validatedName.Error;
        }

        var validatedParticipants = ValidateParticipants(participants);
        if (validatedParticipants.IsFailure)
        {
            return validatedParticipants.Error;
        }

        var reaction = new Reaction(ReactionId.New(), validatedName.Value, type, difficulty, isReversible);

        foreach (var specification in participants)
        {
            reaction._participants.Add(new ReactionParticipant(
                ReactionParticipantId.New(),
                specification.SubstanceId,
                specification.Role,
                specification.Coefficient,
                specification.State));
        }

        reaction.Raise(new ReactionCreated(reaction.Id, reaction.Name));

        return reaction;
    }

    public Result DescribeEnergetics(Thermodynamics energetics)
    {
        Energetics = energetics;

        return Result.Success();
    }

    public Result DescribeConditions(ReactionConditions conditions)
    {
        Conditions = conditions;

        return Result.Success();
    }

    public Result DescribeVisualization(VisualizationHint visualization)
    {
        Visualization = visualization;

        return Result.Success();
    }

    public Result Describe(
        string? description,
        string? mechanism,
        string? safetyWarnings,
        IEnumerable<string>? realWorldExamples,
        IEnumerable<string>? tags)
    {
        Description = Clean(description);
        Mechanism = Clean(mechanism);
        SafetyWarnings = Clean(safetyWarnings);

        Replace(_realWorldExamples, realWorldExamples);
        Replace(_tags, tags);

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

    private static Result ValidateParticipants(IReadOnlyList<ParticipantSpecification> participants)
    {
        var reactants = participants.Where(p => p.Role == ParticipantRole.Reactant).ToList();
        var products = participants.Where(p => p.Role == ParticipantRole.Product).ToList();

        if (reactants.Count == 0)
        {
            return Result.Failure(NoReactants);
        }

        if (products.Count == 0)
        {
            return Result.Failure(NoProducts);
        }

        if (participants.Any(p => p.Coefficient < 1))
        {
            return Result.Failure(NonPositiveCoefficient);
        }

        if (HasDuplicates(reactants) || HasDuplicates(products))
        {
            return Result.Failure(DuplicateParticipant);
        }

        return AtomCounts(reactants).Equals(AtomCounts(products))
            ? Result.Success()
            : Result.Failure(NotMassBalanced);
    }

    private static bool HasDuplicates(List<ParticipantSpecification> side) =>
        side.Select(p => p.SubstanceId).Distinct().Count() != side.Count;

    private static AtomTally AtomCounts(IEnumerable<ParticipantSpecification> side) =>
        new(side
            .SelectMany(participant => participant.Formula.Composition
                .Select(quantity => (quantity.Symbol, Total: quantity.Count * participant.Coefficient)))
            .GroupBy(entry => entry.Symbol)
            .Select(group => (Symbol: group.Key, Total: group.Sum(entry => entry.Total)))
            .OrderBy(entry => entry.Symbol.Value, StringComparer.Ordinal)
            .ToList());

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Replace(List<string> target, IEnumerable<string>? values)
    {
        target.Clear();

        if (values is null)
        {
            return;
        }

        target.AddRange(values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
    }

    private sealed class AtomTally(IReadOnlyList<(ElementSymbol Symbol, int Total)> entries)
    {
        public override bool Equals(object? obj) =>
            obj is AtomTally other
            && entries.Count == other.Entries.Count
            && entries.SequenceEqual(other.Entries);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var (symbol, total) in entries)
            {
                hash.Add(symbol);
                hash.Add(total);
            }

            return hash.ToHashCode();
        }

        private IReadOnlyList<(ElementSymbol Symbol, int Total)> Entries => entries;
    }
}

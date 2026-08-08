using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.DTOs;

public class ReactionDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = default!;

    public string Equation { get; init; } = default!;

    public string? EquationBalanced { get; init; }

    public ReactionType ReactionType { get; init; }

    public string? Category { get; init; }

    public decimal? RequiredTemperature { get; init; }

    public decimal? RequiredPressure { get; init; }

    public bool RequiresCatalyst { get; init; }

    public string? CatalystInfo { get; init; }

    public decimal? EnthalpyChange { get; init; }

    public bool? IsExothermic { get; init; }

    public decimal? ActivationEnergy { get; init; }

    public string? AnimationType { get; init; }

    public string? EffectPreset { get; init; }

    public int? AnimationDurationMs { get; init; }

    public string? Description { get; init; }

    public string? Mechanism { get; init; }

    public string? RealWorldExamples { get; init; }

    public string? SafetyWarnings { get; init; }

    public int DifficultyLevel { get; init; }

    public IReadOnlyList<ReactionParticipantDto> Reactants { get; init; } = [];

    public IReadOnlyList<ReactionParticipantDto> Products { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];
}

public class ReactionSummaryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = default!;

    public string Equation { get; init; } = default!;

    public ReactionType ReactionType { get; init; }

    public string? Category { get; init; }

    public bool? IsExothermic { get; init; }

    public int DifficultyLevel { get; init; }
}

public class ReactionParticipantDto
{
    public Guid Id { get; init; }

    public Guid? ElementId { get; init; }

    public string? ElementSymbol { get; init; }

    public string? ElementName { get; init; }

    public Guid? MoleculeId { get; init; }

    public string? MoleculeFormula { get; init; }

    public string? MoleculeName { get; init; }

    public ParticipantRole Role { get; init; }

    public int Coefficient { get; init; }

    public MatterState? State { get; init; }
}

public class CreateReactionDto
{
    public string Name { get; init; } = default!;

    public string Equation { get; init; } = default!;

    public string? EquationBalanced { get; init; }

    public ReactionType ReactionType { get; init; }

    public string? Category { get; init; }

    public decimal? RequiredTemperature { get; init; }

    public decimal? RequiredPressure { get; init; }

    public bool RequiresCatalyst { get; init; }

    public string? CatalystInfo { get; init; }

    public decimal? EnthalpyChange { get; init; }

    public bool? IsExothermic { get; init; }

    public decimal? ActivationEnergy { get; init; }

    public string? AnimationType { get; init; }

    public string? EffectPreset { get; init; }

    public int? AnimationDurationMs { get; init; }

    public string? Description { get; init; }

    public string? Mechanism { get; init; }

    public string? RealWorldExamples { get; init; }

    public string? SafetyWarnings { get; init; }

    public int DifficultyLevel { get; init; }

    public IReadOnlyList<CreateReactionParticipantDto>? Participants { get; init; }

    public IReadOnlyList<string>? Tags { get; init; }
}

public class CreateReactionParticipantDto
{
    public Guid? ElementId { get; init; }

    public Guid? MoleculeId { get; init; }

    public ParticipantRole Role { get; init; }

    public int Coefficient { get; init; }

    public MatterState? State { get; init; }
}

public class UpdateReactionDto
{
    public string Name { get; init; } = default!;

    public string Equation { get; init; } = default!;

    public string? EquationBalanced { get; init; }

    public ReactionType ReactionType { get; init; }

    public string? Category { get; init; }

    public decimal? RequiredTemperature { get; init; }

    public decimal? RequiredPressure { get; init; }

    public bool RequiresCatalyst { get; init; }

    public string? CatalystInfo { get; init; }

    public decimal? EnthalpyChange { get; init; }

    public bool? IsExothermic { get; init; }

    public decimal? ActivationEnergy { get; init; }

    public string? AnimationType { get; init; }

    public string? EffectPreset { get; init; }

    public int? AnimationDurationMs { get; init; }

    public string? Description { get; init; }

    public string? Mechanism { get; init; }

    public string? RealWorldExamples { get; init; }

    public string? SafetyWarnings { get; init; }

    public int DifficultyLevel { get; init; }
}

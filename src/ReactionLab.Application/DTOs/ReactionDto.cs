using ReactionLab.Domain.Enums;

namespace ReactionLab.Application.DTOs;

public record ReactionDto(
    Guid Id,
    string Name,
    string Equation,
    string? EquationBalanced,
    ReactionType ReactionType,
    string? Category,
    decimal? RequiredTemperature,
    decimal? RequiredPressure,
    bool RequiresCatalyst,
    string? CatalystInfo,
    decimal? EnthalpyChange,
    bool? IsExothermic,
    decimal? ActivationEnergy,
    string? AnimationType,
    string? EffectPreset,
    int? AnimationDurationMs,
    string? Description,
    string? Mechanism,
    string? RealWorldExamples,
    string? SafetyWarnings,
    int DifficultyLevel,
    IReadOnlyList<ReactionParticipantDto> Reactants,
    IReadOnlyList<ReactionParticipantDto> Products,
    IReadOnlyList<string> Tags
);

public record ReactionSummaryDto(
    Guid Id,
    string Name,
    string Equation,
    ReactionType ReactionType,
    string? Category,
    bool? IsExothermic,
    int DifficultyLevel
);

public record ReactionParticipantDto(
    Guid Id,
    Guid? ElementId,
    string? ElementSymbol,
    string? ElementName,
    Guid? MoleculeId,
    string? MoleculeFormula,
    string? MoleculeName,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State
);

public record CreateReactionDto(
    string Name,
    string Equation,
    string? EquationBalanced,
    ReactionType ReactionType,
    string? Category,
    decimal? RequiredTemperature,
    decimal? RequiredPressure,
    bool RequiresCatalyst,
    string? CatalystInfo,
    decimal? EnthalpyChange,
    bool? IsExothermic,
    decimal? ActivationEnergy,
    string? AnimationType,
    string? EffectPreset,
    int? AnimationDurationMs,
    string? Description,
    string? Mechanism,
    string? RealWorldExamples,
    string? SafetyWarnings,
    int DifficultyLevel,
    IReadOnlyList<CreateReactionParticipantDto>? Participants,
    IReadOnlyList<string>? Tags
);

public record CreateReactionParticipantDto(
    Guid? ElementId,
    Guid? MoleculeId,
    ParticipantRole Role,
    int Coefficient,
    MatterState? State
);

public record UpdateReactionDto(
    string Name,
    string Equation,
    string? EquationBalanced,
    ReactionType ReactionType,
    string? Category,
    decimal? RequiredTemperature,
    decimal? RequiredPressure,
    bool RequiresCatalyst,
    string? CatalystInfo,
    decimal? EnthalpyChange,
    bool? IsExothermic,
    decimal? ActivationEnergy,
    string? AnimationType,
    string? EffectPreset,
    int? AnimationDurationMs,
    string? Description,
    string? Mechanism,
    string? RealWorldExamples,
    string? SafetyWarnings,
    int DifficultyLevel
);
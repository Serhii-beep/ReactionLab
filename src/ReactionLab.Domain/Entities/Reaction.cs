using ReactionLab.Domain.Common;
using ReactionLab.Domain.Enums;

namespace ReactionLab.Domain.Entities;

public class Reaction : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Equation { get; set; } = string.Empty;

    public string? EquationBalanced { get; set; }

    public ReactionType ReactionType { get; set; }

    public string? Category { get; set; }

    // Conditions
    public decimal? RequiredTemperature { get; set; } // Kelvin

    public decimal? RequiredPressure { get; set; } // kPa

    public bool RequiresCatalyst { get; set; }

    public string? CatalystInfo { get; set; }

    // Thermodynamics
    public decimal? EnthalpyChange { get; set; } // kJ/mol

    public bool? IsExothermic { get; set; }

    public decimal? ActivationEnergy { get; set; }

    // Animation/Visual
    public string? AnimationType { get; set; }

    public string? EffectPreset { get; set; }

    public int? AnimationDurationMs { get; set; }

    // Educational
    public string? Description { get; set; }

    public string? Mechanism { get; set; }

    public string? RealWorldExamples { get; set; } // JSON array

    public string? SafetyWarnings { get; set; }

    public int DifficultyLevel { get; set; } = 1; // 1-5

    public ICollection<ReactionParticipant> Participants { get; set; } = [];

    public ICollection<ReactionTag> ReactionTags { get; set; } = [];
}
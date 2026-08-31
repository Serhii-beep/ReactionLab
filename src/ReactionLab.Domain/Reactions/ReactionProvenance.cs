using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reactions;

public sealed record ReactionProvenance
{
    public const string CuratedRule = "curated";

    public const int MaximumRuleLength = 60;

    public static readonly Error RuleRequired = Error.Validation(
        "ReactionProvenance.RuleRequired",
        "A reaction must record the rule that produces it.");

    public static readonly Error RuleTooLong = Error.Validation(
        "ReactionProvenance.RuleTooLong",
        $"A rule code must not exceed {MaximumRuleLength} characters.")
        .WithArgs(("max", MaximumRuleLength));

    public static readonly Error ConfidenceOutOfRange = Error.Validation(
        "ReactionProvenance.ConfidenceOutOfRange",
        "Confidence must be between 0 and 1.");

    private ReactionProvenance(string rule, decimal confidence)
    {
        Rule = rule;
        Confidence = confidence;
    }

    public string Rule { get; }

    public decimal Confidence { get; }

    public bool IsCurated => string.Equals(Rule, CuratedRule, StringComparison.Ordinal);

    public static ReactionProvenance Curated { get; } = new(CuratedRule, 1m);

    public static Result<ReactionProvenance> Create(string? rule, decimal confidence)
    {
        if (string.IsNullOrWhiteSpace(rule))
        {
            return RuleRequired;
        }

        var trimmed = rule.Trim();

        if (trimmed.Length > MaximumRuleLength)
        {
            return RuleTooLong;
        }

        return confidence is < 0m or > 1m
            ? ConfidenceOutOfRange
            : new ReactionProvenance(trimmed, confidence);
    }
}

using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Elements;

public sealed record PeriodicPosition
{
    public const int MinimumPeriod = 1;
    public const int MaximumPeriod = 7;
    public const int MinimumGroup = 1;
    public const int MaximumGroup = 18;

    public static readonly Error PeriodOutOfRange = Error.Validation(
        "PeriodicPosition.PeriodOutOfRange",
        $"Period must be between {MinimumPeriod} and {MaximumPeriod}.");

    public static readonly Error GroupOutOfRange = Error.Validation(
        "PeriodicPosition.GroupOutOfRange",
        $"Group must be between {MinimumGroup} and {MaximumGroup}.");

    private PeriodicPosition(int period, int? group)
    {
        Period = period;
        Group = group;
    }

    public int Period { get; }

    public int? Group { get; }

    public bool IsFBlock => Group is null;

    public static Result<PeriodicPosition> Create(int period, int? group)
    {
        if (period is < MinimumPeriod or > MaximumPeriod)
        {
            return PeriodOutOfRange;
        }

        if (group is < MinimumGroup or > MaximumGroup)
        {
            return GroupOutOfRange;
        }

        return new PeriodicPosition(period, group);
    }

    public override string ToString() => Group is null ? $"Period {Period}" : $"Period {Period}, Group {Group}";
}

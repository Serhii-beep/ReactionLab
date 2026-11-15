namespace ReactionLab.Identity.Persistence.Maintenance;

public sealed class MaintenanceRun
{
    public const int MaximumNameLength = 64;

    private MaintenanceRun()
    {

    }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset DueAt { get; private set; }

    public DateTimeOffset? LeasedUntil { get; private set; }
}

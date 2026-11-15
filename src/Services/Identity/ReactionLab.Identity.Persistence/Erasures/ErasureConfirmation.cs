namespace ReactionLab.Identity.Persistence.Erasures;

public sealed class ErasureConfirmation
{
    private ErasureConfirmation()
    {

    }

    public Guid UserId { get; private set; }

    public string Service { get; private set; } = string.Empty;

    public DateTimeOffset? ConfirmedAt { get; private set; }
}

namespace ReactionLab.Messaging.Consuming;

public sealed class ErasureMarker
{
    private ErasureMarker()
    {

    }

    public string Subject { get; private init; } = string.Empty;

    public long Version { get; private init; }

    public DateTimeOffset ErasedAt { get; private init; }
}

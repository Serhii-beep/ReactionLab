namespace ReactionLab.SharedKernel;

public static class EventIds
{
    private static readonly Lock Gate = new();

    private static Guid _lastEventId;

    public static Guid Next()
    {
        var candidate = Guid.CreateVersion7();

        lock (Gate)
        {
            _lastEventId = candidate.CompareTo(_lastEventId) > 0 ? candidate : Successor(_lastEventId);

            return _lastEventId;
        }
    }

    private static Guid Successor(Guid eventId)
    {
        Span<byte> bytes = stackalloc byte[16];
        eventId.TryWriteBytes(bytes, bigEndian: true, out _);

        var index = bytes.Length - 1;

        while (++bytes[index] == 0)
        {
            index--;
        }

        return new Guid(bytes, bigEndian: true);
    }
}

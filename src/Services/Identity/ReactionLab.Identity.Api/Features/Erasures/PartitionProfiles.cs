namespace ReactionLab.Identity.Api.Features.Erasures;

internal sealed class PartitionProfiles
{
    public HashSet<Guid> CanonicalAccountIds { get; } = [];

    public HashSet<string> OtherKeys { get; } = new(StringComparer.Ordinal);

    public int Count => CanonicalAccountIds.Count + OtherKeys.Count;

    public void Record(string key, Guid accountId, bool isProfile)
    {
        if (key == accountId.ToString())
        {
            Keep(CanonicalAccountIds, accountId, isProfile);
        }
        else
        {
            Keep(OtherKeys, key, isProfile);
        }
    }

    private static void Keep<TKey>(HashSet<TKey> keys, TKey key, bool isProfile)
    {
        if (isProfile)
        {
            keys.Add(key);
        }
        else
        {
            keys.Remove(key);
        }
    }
}

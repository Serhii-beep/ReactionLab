using System.Buffers;
using System.Collections.Concurrent;
using ReactionLab.Catalog.Application.Common.Abstractions;

namespace ReactionLab.Catalog.Infrastructure.Releases.Matching;

internal sealed class ReactionMatchIndex
{
    private readonly Dictionary<Guid, int> _substances;
    private readonly Guid[] _reactions;
    private readonly byte[] _reactantCounts;
    private readonly int[] _offsets;
    private readonly int[] _postings;
    private readonly ConcurrentBag<byte[]> _counters = [];

    private ReactionMatchIndex(Dictionary<Guid, int> substances, Guid[] reactions, byte[] reactantCounts, int[] offsets, int[] postings)
    {
        _substances = substances;
        _reactions = reactions;
        _reactantCounts = reactantCounts;
        _offsets = offsets;
        _postings = postings;
    }

    public int Reactions => _reactions.Length;

    public long Bytes =>
        (_reactions.Length * 16L) + _reactantCounts.Length + (_offsets.Length * 4L) + (_postings.Length * 4L) + (_substances.Count * 24L);

    public static ReactionMatchIndex Build(IEnumerable<(Guid ReactionId, Guid[] Reactants)> reactionsInRankOrder)
    {
        var substances = new Dictionary<Guid, int>();
        var reactions = new List<Guid>();
        var reactantCounts = new List<byte>();
        var reactants = new List<int[]>();
        var postings = 0;

        foreach (var (reactionId, reactantIds) in reactionsInRankOrder)
        {
            var dense = reactantIds.Distinct().Select(id => DenseIdOf(substances, id)).ToArray();

            if (dense.Length is 0 or > byte.MaxValue)
            {
                continue;
            }

            reactions.Add(reactionId);
            reactantCounts.Add((byte)dense.Length);
            reactants.Add(dense);
            postings += dense.Length;
        }

        var offsets = new int[substances.Count + 1];

        foreach (var substance in reactants.SelectMany(dense => dense))
        {
            offsets[substance + 1]++;
        }

        for (var substance = 0; substance < substances.Count; substance++)
        {
            offsets[substance + 1] += offsets[substance];
        }

        var flat = new int[postings];
        var fill = (int[])offsets.Clone();

        for (var reaction = 0; reaction < reactants.Count; reaction++)
        {
            foreach (var substance in reactants[reaction])
            {
                flat[fill[substance]++] = reaction;
            }
        }

        return new ReactionMatchIndex(substances, [.. reactions], [.. reactantCounts], offsets, flat);
    }

    public IReadOnlyList<MatchedReaction> Match(IReadOnlyCollection<Guid> available, ReactantMatch match, int limit)
    {
        var bench = available
            .Select(id => _substances.TryGetValue(id, out var dense) ? dense : -1)
            .Where(dense => dense >= 0)
            .Distinct()
            .ToArray();

        if (bench.Length == 0 || limit <= 0)
        {
            return [];
        }

        var postings = bench.Sum(substance => _offsets[substance + 1] - _offsets[substance]);
        var touched = ArrayPool<int>.Shared.Rent(postings);
        var matched = _counters.TryTake(out var counter) ? counter : new byte[_reactions.Length];

        var clean = false;

        try
        {
            var touchedCount = 0;

            foreach (var substance in bench)
            {
                for (var posting = _offsets[substance]; posting < _offsets[substance + 1]; posting++)
                {
                    var reaction = _postings[posting];

                    if (matched[reaction]++ == 0)
                    {
                        touched[touchedCount++] = reaction;
                    }
                }
            }

            var best = Best(touched.AsSpan(0, touchedCount), matched, match, limit);

            clean = true;

            return best;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(touched);

            if (clean)
            {
                _counters.Add(matched);
            }
        }
    }

    private List<MatchedReaction> Best(Span<int> touched, byte[] matched, ReactantMatch match, int limit)
    {
        var heap = new long[limit];
        var count = 0;

        foreach (var reaction in touched)
        {
            var missing = _reactantCounts[reaction] - matched[reaction];

            matched[reaction] = 0;

            if (match == ReactantMatch.Complete && missing != 0)
            {
                continue;
            }

            var key = ((long)missing * _reactions.Length) + reaction;

            if (count < limit)
            {
                heap[count++] = key;
                SiftUp(heap, count - 1);
            }
            else if (key < heap[0])
            {
                heap[0] = key;
                SiftDown(heap, count);
            }
        }

        Array.Sort(heap, 0, count);

        var best = new List<MatchedReaction>(count);

        for (var position = 0; position < count; position++)
        {
            best.Add(new MatchedReaction(_reactions[heap[position] % _reactions.Length], (int)(heap[position] / _reactions.Length)));
        }

        return best;
    }

    private static void SiftUp(long[] heap, int position)
    {
        while (position > 0)
        {
            var parent = (position - 1) / 2;

            if (heap[parent] >= heap[position])
            {
                return;
            }

            (heap[parent], heap[position]) = (heap[position], heap[parent]);
            position = parent;
        }
    }

    private static void SiftDown(long[] heap, int count)
    {
        var position = 0;

        while (true)
        {
            var left = (position * 2) + 1;

            if (left >= count)
            {
                return;
            }

            var largest = left + 1 < count && heap[left + 1] > heap[left] ? left + 1 : left;

            if (heap[position] >= heap[largest])
            {
                return;
            }

            (heap[position], heap[largest]) = (heap[largest], heap[position]);
            position = largest;
        }
    }

    private static int DenseIdOf(Dictionary<Guid, int> substances, Guid id)
    {
        if (!substances.TryGetValue(id, out var dense))
        {
            dense = substances.Count;
            substances[id] = dense;
        }

        return dense;
    }
}

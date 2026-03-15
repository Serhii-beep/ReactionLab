namespace ReactionLab.Application.Common;

public static class CacheKeys
{
    private const string Prefix = "reactionlab";

    public static class Elements
    {
        private const string Section = $"{Prefix}:elements";

        public const string All = $"{Section}:all";
        
        public static string ById(Guid id) => $"{Section}:id:{id}";

        public static string BySymbol(string symbol) => $"{Section}:symbol:{symbol.ToUpperInvariant()}";

        public static string Search(string query) => $"{Section}:search:{query.ToLowerInvariant()}";
    }

    public static class Molecules
    {
        private const string Section = $"{Prefix}:molecules";

        public const string All = $"{Section}:all";

        public const string Popular = $"{Section}:popular";

        public static string ById(Guid id) => $"{Section}:id:{id}";

        public static string ByFormula(string formula) => $"{Section}:formula:{formula.ToUpperInvariant()}";

        public static string Search(string query, long cursorTicks, int pageSize) => $"{Section}:search:{query.ToLowerInvariant()}:c{cursorTicks}:s{pageSize}";

        public static string UsageCount(Guid id) => $"{Section}:usage:{id}";
    }

    public static class Reactions
    {
        private const string Section = $"{Prefix}:reactions";

        public const string All = $"{Section}:all";

        public static string ById(Guid id) => $"{Section}:id:{id}";

        public static string ByType(int type) => $"{Section}:type:{type}";

        public static string Search(string query) => $"{Section}:search:{query.ToLowerInvariant()}";

        public static string Available(
            IEnumerable<Guid> moleculeIds,
            IEnumerable<Guid> elementIds,
            string? searchTerm,
            long cursorTicks,
            int pageSize)
        {
            var molHash = string.Join("-", moleculeIds.OrderBy(id => id).Select(id => id.ToString("N")[..8]));
            var elHash = string.Join("-", elementIds.OrderBy(id => id).Select(id => id.ToString("N")[..8]));
            var search = searchTerm?.ToLowerInvariant() ?? "all";
            return $"{Section}:available:{molHash}:{elHash}:{search}:c{cursorTicks}:s{pageSize}";
        }
    }
}
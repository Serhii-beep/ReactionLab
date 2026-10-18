namespace ReactionLab.CatalogBuilder;

internal static class CommandLineOptions
{
    public static string? ValueOf(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);

        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}

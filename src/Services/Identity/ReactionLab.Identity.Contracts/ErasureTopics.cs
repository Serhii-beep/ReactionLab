namespace ReactionLab.Identity.Contracts;

public static class ErasureTopics
{
    public const string ConfirmationsPrefix = "erasure.confirmations.";

    public static string ConfirmationsOf(string service) => ConfirmationsPrefix + service;

    public static string? ServiceOf(string confirmationsTopic) =>
        confirmationsTopic.StartsWith(ConfirmationsPrefix, StringComparison.Ordinal)
            ? confirmationsTopic[ConfirmationsPrefix.Length..]
            : null;
}

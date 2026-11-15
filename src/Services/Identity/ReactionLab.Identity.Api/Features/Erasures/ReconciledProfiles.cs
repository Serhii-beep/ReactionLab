namespace ReactionLab.Identity.Api.Features.Erasures;

internal sealed record ReconciledProfiles(int PartitionCount, long ProfileCount, int TombstonedCount);

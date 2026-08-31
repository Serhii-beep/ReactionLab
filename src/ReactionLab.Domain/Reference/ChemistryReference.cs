using ReactionLab.Domain.Common;

namespace ReactionLab.Domain.Reference;

public sealed class ChemistryReference : AggregateRoot<ChemistryReferenceId>
{
    public const int MaximumPayloadLength = 1000000;

    public static readonly Error PayloadRequired = Error.Validation(
        "ChemistryReference.PayloadRequired",
        "A reference dataset must carry a payload.");

    public static readonly Error PayloadTooLarge = Error.Validation(
        "ChemistryReference.PayloadTooLarge",
        $"A reference payload must not exceed {MaximumPayloadLength} characters.")
        .WithArgs(("max", MaximumPayloadLength));

    private ChemistryReference(ChemistryReferenceId id, ReferenceKey key, string payload) : base(id)
    {
        Key = key;
        Payload = payload;
    }

    private ChemistryReference()
    {

    }

    public ReferenceKey Key { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public static Result<ChemistryReference> Create(ReferenceKey key, string? payload)
    {
        var validated = Validate(payload);

        return validated.IsFailure
            ? validated.Error
            : new ChemistryReference(ChemistryReferenceId.New(), key, validated.Value);
    }

    public Result Replace(string? payload)
    {
        var validated = Validate(payload);

        if (validated.IsFailure)
        {
            return Result.Failure(validated.Error);
        }

        Payload = validated.Value;

        return Result.Success();
    }

    private static Result<string> Validate(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return PayloadRequired;
        }

        var trimmed = payload.Trim();

        return trimmed.Length > MaximumPayloadLength ? PayloadTooLarge : trimmed;
    }
}

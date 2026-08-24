using System.Buffers.Binary;
using System.Buffers.Text;
using ReactionLab.Domain.Common;

namespace ReactionLab.Application.Common.Pagination;

public sealed record Cursor
{
    public static readonly Error Malformed = Error.Validation(
        "Cursor.Malformed",
        "The pagination cursor is not valid.",
        field: "Cursor");

    private const byte KeysetKind = 1;
    private const byte OffsetKind = 2;
    private const int PayloadBytes = 17;
    private const int EncodedLength = 23;

    private Cursor(byte kind, Guid afterId, int skip)
    {
        Kind = kind;
        AfterId = afterId;
        Skip = skip;
    }

    public Guid AfterId { get; }

    public int Skip { get; }

    public bool IsKeyset => Kind == KeysetKind;

    private byte Kind { get; }

    public static Cursor After(Guid id) => new(KeysetKind, id, 0);

    public static Cursor Skipping(int rows) => new(OffsetKind, Guid.Empty, rows);

    public string Encode()
    {
        Span<byte> bytes = stackalloc byte[PayloadBytes];
        bytes[0] = Kind;

        if (Kind == KeysetKind)
        {
            AfterId.TryWriteBytes(bytes[1..]);
        }
        else
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes[1..], Skip);
        }

        return Base64Url.EncodeToString(bytes);
    }

    public static Result<Cursor> Decode(string? encoded)
    {
        if (encoded is not { Length: EncodedLength } || !Base64Url.IsValid(encoded))
        {
            return Malformed;
        }

        Span<byte> bytes = stackalloc byte[PayloadBytes];

        if (!Base64Url.TryDecodeFromChars(encoded, bytes, out var written) || written != PayloadBytes)
        {
            return Malformed;
        }

        return bytes[0] switch
        {
            KeysetKind => new Cursor(KeysetKind, new Guid(bytes[1..]), 0),
            OffsetKind => new Cursor(OffsetKind, Guid.Empty, BinaryPrimitives.ReadInt32LittleEndian(bytes[1..])),
            _ => Malformed
        };
    }
}

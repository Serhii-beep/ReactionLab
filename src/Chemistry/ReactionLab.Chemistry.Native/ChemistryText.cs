using System.Runtime.InteropServices;
using System.Text;

namespace ReactionLab.Chemistry.Native;

[StructLayout(LayoutKind.Sequential)]
internal readonly unsafe struct ChemistryText
{
    private ChemistryText(byte* bytes, nuint length)
    {
        Bytes = bytes;
        Length = length;
    }

    public byte* Bytes { get; }

    public nuint Length { get; }

    public static ChemistryText Of(string text)
    {
        var length = Encoding.UTF8.GetByteCount(text);
        var bytes = (byte*)NativeMemory.Alloc((nuint)length);

        Encoding.UTF8.GetBytes(text, new Span<byte>(bytes, length));

        return new ChemistryText(bytes, (nuint)length);
    }
}

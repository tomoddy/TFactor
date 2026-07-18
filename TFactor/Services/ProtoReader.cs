namespace TFactor.Services;

/// <summary>
/// A minimal Protocol Buffers wire-format reader, just enough to parse the two message types used by Google Authenticator's export payload. Avoids pulling in a full protobuf library for such a small, fixed schema.
/// </summary>
internal static class ProtoReader
{
    /// <summary>
    /// Wire type for varint-encoded values.
    /// </summary>
    public const int WireTypeVarint = 0;

    /// <summary>
    /// Wire type for 64-bit fixed values.
    /// </summary>
    public const int WireTypeFixed64 = 1;

    /// <summary>
    /// Wire type for length-delimited values (strings, bytes, embedded messages).
    /// </summary>
    public const int WireTypeLengthDelimited = 2;

    /// <summary>
    /// Wite type for 32-bit fixed values.
    /// </summary>
    public const int WireTypeFixed32 = 5;

    /// <summary>
    /// Reads a field tag (field number + wire type) at the current position and advances past it.
    /// </summary>
    /// <param name="data">The buffer being read</param>
    /// <param name="pos">The current read position, advanced past the tag</param>
    /// <returns>The field number and wire type encoded in the tag</returns>
    public static (int FieldNumber, int WireType) ReadTag(byte[] data, ref int pos)
    {
        ulong tag = ReadVarint(data, ref pos);
        return ((int)(tag >> 3), (int)(tag & 0x7));
    }

    /// <summary>
    /// Reads a length-delimited field (wire type 2) at the current position and advances past it.
    /// </summary>
    /// <param name="data">The buffer being read</param>
    /// <param name="pos">The current read position, advanced past the field</param>
    /// <returns>The bytes contained in the field</returns>
    public static byte[] ReadLengthDelimited(byte[] data, ref int pos)
    {
        int length = (int)ReadVarint(data, ref pos);
        byte[] slice = data[pos..(pos + length)];
        pos += length;
        return slice;
    }

    /// <summary>
    /// Reads a varint-encoded value at the current position and advances past it.
    /// </summary>
    /// <param name="data">The buffer being read</param>
    /// <param name="pos">The current read position, advanced past the varint</param>
    /// <returns>The decoded value</returns>
    public static ulong ReadVarint(byte[] data, ref int pos)
    {
        ulong result = 0;
        int shift = 0;
        while (true)
        {
            byte b = data[pos++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                break;
            }
            shift += 7;
        }
        return result;
    }

    /// <summary>
    /// Skips over a field of the given wire type at the current position, for fields we don't care about.
    /// </summary>
    /// <param name="data">The buffer being read</param>
    /// <param name="pos">The current read position, advanced past the field</param>
    /// <param name="wireType">The wire type of the field to skip</param>
    /// <exception cref="NotSupportedException">Thrown for an unrecognized wire type.</exception>
    public static void SkipField(byte[] data, ref int pos, int wireType)
    {
        switch (wireType)
        {
            case WireTypeVarint:
                ReadVarint(data, ref pos);
                break;

            case WireTypeFixed64:
                pos += 8;
                break;

            case WireTypeLengthDelimited:
                ReadLengthDelimited(data, ref pos);
                break;

            case WireTypeFixed32:
                pos += 4;
                break;

            default:
                throw new NotSupportedException($"Unsupported protobuf wire type: {wireType}");
        }
    }
}
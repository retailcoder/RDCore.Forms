// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace RDCore.Forms.Import.VB6;

// A designer file's companion binary file (.frx, .ctx or .pgx): the records its properties cite by offset. Each reader takes the
// record at an offset in the shape its property is written in, and fails on a record that runs past the end of the file.
internal sealed class Companion(byte[] bytes, Encoding encoding)
{
    // "lt\0\0": the signature of a picture record.
    private const uint PictureSignature = 0x0000746C;

    // A string cited as $"Form1.frx":0000: a 4-byte length, then the string's bytes.
    public bool TryReadLongString(int offset, out string value)
    {
        value = "";
        if (!TryReadInt32(offset, out var length) || !TryGetSpan(offset + 4, length, out var text))
        {
            return false;
        }

        value = encoding.GetString(text);
        return true;
    }

    // A string cited as "Form1.frx":0000: a 1-byte length, then the string's bytes, of which VB6 reads up to the first NUL.
    public bool TryReadShortString(int offset, out string value)
    {
        value = "";
        if (!TryGetSpan(offset, 1, out var prefix) || !TryGetSpan(offset + 1, prefix[0], out var text))
        {
            return false;
        }

        var nul = text.IndexOf((byte)0);
        value = encoding.GetString(nul < 0 ? text : text[..nul]);
        return true;
    }

    // A picture record: a 4-byte length of the rest, the picture signature, the image's 4-byte length, then the image file. An
    // image of no bytes is no picture.
    public bool TryReadPicture(int offset, out byte[]? image)
    {
        image = null;
        if (!TryReadInt32(offset, out var length) || length < 8
            || !TryReadInt32(offset + 4, out var signature) || unchecked((uint)signature) != PictureSignature
            || !TryReadInt32(offset + 8, out var imageLength) || imageLength != length - 8
            || !TryGetSpan(offset + 12, imageLength, out var file))
        {
            return false;
        }

        image = imageLength == 0 ? null : file.ToArray();
        return true;
    }

    // A list record: a 2-byte count; when the count is not 0, a 2-byte word VB6 does not read; then each item as a 2-byte length
    // and its bytes.
    public bool TryReadList(int offset, out IReadOnlyList<string> items)
    {
        items = [];
        if (!TryReadUInt16(offset, out var count))
        {
            return false;
        }

        var list = new List<string>(count);
        var at = offset + (count == 0 ? 2 : 4);
        for (var i = 0; i < count; i++)
        {
            if (!TryReadUInt16(at, out var length) || !TryGetSpan(at + 2, length, out var item))
            {
                return false;
            }

            list.Add(encoding.GetString(item));
            at += 2 + length;
        }

        items = list;
        return true;
    }

    // An item data record: a list record whose items are each Long's decimal text, which VB6 reads up to the first NUL.
    public bool TryReadItemData(int offset, out IReadOnlyList<int> values)
    {
        values = [];
        if (!TryReadList(offset, out var items))
        {
            return false;
        }

        var list = new int[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            var text = items[i];
            var nul = text.IndexOf('\0');
            if (!int.TryParse(nul < 0 ? text : text[..nul], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out list[i]))
            {
                return false;
            }
        }

        values = list;
        return true;
    }

    private bool TryGetSpan(int offset, int length, out ReadOnlySpan<byte> span)
    {
        // Compared by subtraction, so a length near int.MaxValue cannot overflow past the bound.
        if (offset < 0 || length < 0 || offset > bytes.Length || length > bytes.Length - offset)
        {
            span = default;
            return false;
        }

        span = bytes.AsSpan(offset, length);
        return true;
    }

    private bool TryReadInt32(int offset, out int value)
    {
        value = 0;
        if (!TryGetSpan(offset, 4, out var span))
        {
            return false;
        }

        value = BinaryPrimitives.ReadInt32LittleEndian(span);
        return true;
    }

    private bool TryReadUInt16(int offset, out int value)
    {
        value = 0;
        if (!TryGetSpan(offset, 2, out var span))
        {
            return false;
        }

        value = BinaryPrimitives.ReadUInt16LittleEndian(span);
        return true;
    }
}

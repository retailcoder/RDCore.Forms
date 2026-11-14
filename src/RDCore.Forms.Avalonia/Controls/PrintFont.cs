using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// A form font as the Print method draws it, in whole pixels: each glyph's advance is rounded, as a GDI font's are, and there is no
// kerning, so a text is as wide drawn as measured. The column is the font's average character width, as VB6 uses its tmAveCharWidth:
// the OS/2 table's, at the font's size. A line is the font's ascent and descent together.
internal sealed class PrintFont
{
    private static readonly ConcurrentDictionary<FormFont, PrintFont> fonts = new();

    private readonly GlyphTypeface typeface;
    private readonly double em;
    private readonly double unit;

    private PrintFont(FormFont font)
    {
        typeface = new Typeface(FormFonts.ToFontFamily(font), font.Italic ? FontStyle.Italic : FontStyle.Normal,
            font.Bold ? FontWeight.Bold : FontWeight.Normal).GlyphTypeface;
        em = FormFonts.ToFontSize(font);
        var metrics = typeface.Metrics;
        unit = em / Math.Max(1, (int)metrics.DesignEmHeight);
        Ascent = (int)Math.Round(Math.Abs(metrics.Ascent) * unit);
        Line = Math.Max(1, (int)Math.Ceiling((Math.Abs(metrics.Ascent) + Math.Abs(metrics.Descent)) * unit - 1e-9));
        Column = Math.Max(1, AverageWidth() is > 0 and var average
            ? (int)Math.Round(average * unit, MidpointRounding.AwayFromZero)
            : Advance(Glyph('x')));
    }

    // The width of a column, in pixels.
    public int Column { get; }

    // The height of a line, in pixels.
    public int Line { get; }

    // The baseline's distance from a line's top, in pixels.
    public int Ascent { get; }

    public static PrintFont Of(FormFont font) => fonts.GetOrAdd(font, key => new PrintFont(key));

    // The lines of a text: a carriage return, a line feed, or both together end one.
    public static string[] Lines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    // The width of a line, in pixels.
    public int Width(string line)
    {
        var width = 0;
        foreach (var rune in line.EnumerateRunes())
            width += Advance(Glyph(rune.Value));
        return width;
    }

    // The glyphs of a line, its top left corner at a pixel.
    public GlyphRun Run(string line, int left, int top)
    {
        var glyphs = new List<GlyphInfo>();
        var cluster = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var glyph = Glyph(rune.Value);
            glyphs.Add(new GlyphInfo(glyph, cluster, Advance(glyph), default));
            cluster += rune.Utf16SequenceLength;
        }
        return new GlyphRun(typeface, em, line.AsMemory(), glyphs, new Point(left, top + Ascent));
    }

    private ushort Glyph(int codepoint) => typeface.CharacterToGlyphMap.TryGetGlyph(codepoint, out var glyph) ? glyph : (ushort)0;

    private int Advance(ushort glyph) =>
        typeface.TryGetHorizontalGlyphAdvance(glyph, out var advance) ? (int)Math.Round(advance * unit, MidpointRounding.AwayFromZero) : 0;

    // The OS/2 table's xAvgCharWidth, in font units, or 0 where the font file has none: a collection's faces are not told apart.
    private int AverageWidth()
    {
        if (!typeface.PlatformTypeface.TryGetStream(out var stream))
            return 0;
        using (stream)
        {
            Span<byte> header = stackalloc byte[12];
            if (stream.ReadAtLeast(header, 12, throwOnEndOfStream: false) < 12 || header[..4].SequenceEqual("ttcf"u8))
                return 0;
            var tables = BinaryPrimitives.ReadUInt16BigEndian(header[4..]);
            Span<byte> record = stackalloc byte[16];
            for (var i = 0; i < tables; i++)
            {
                if (stream.ReadAtLeast(record, 16, throwOnEndOfStream: false) < 16)
                    return 0;
                if (!record[..4].SequenceEqual("OS/2"u8))
                    continue;
                if (!stream.CanSeek)
                    return 0;
                stream.Position = BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
                Span<byte> start = stackalloc byte[4];
                return stream.ReadAtLeast(start, 4, throwOnEndOfStream: false) < 4 ? 0 : BinaryPrimitives.ReadInt16BigEndian(start[2..]);
            }
            return 0;
        }
    }
}

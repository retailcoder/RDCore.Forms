using System.Xml;
using System.Xml.Linq;

namespace RDCore.Forms.Xaml;

/// <summary>
/// Where a document's text names a component: the value of its <c>Name</c> attribute, between the quotes.
/// </summary>
/// <param name="Offset">The value's first character, as an offset in the document's text.</param>
/// <param name="Length">The value's length in the text, its character references included.</param>
/// <param name="Line">The line of the value's first character, the first line being 1.</param>
/// <param name="Column">The column of the value's first character in its line, in UTF-16 code units, the first column being 1.</param>
public readonly record struct FormXamlNameRange(int Offset, int Length, int Line, int Column);

// Offsets in a document's text, from the line and column XML reports.
internal static class FormXamlText
{
    // The offset of each line's first character. A line ends as XML ends it: at a CR LF, a CR or an LF.
    public static int[] LineStarts(string text)
    {
        var starts = new List<int> { 0 };
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                index++;
            if (text[index] is '\r' or '\n')
                starts.Add(index + 1);
        }

        return [.. starts];
    }

    // The range of an attribute's value, from where its name starts: past the name, the equals sign and the opening quote, up to the
    // closing quote, which a value cannot hold as itself. Null where the text is not what the reader read, which a loaded document
    // never gives.
    public static FormXamlNameRange? ValueRange(string text, int[] lines, XAttribute attribute)
    {
        if (attribute is not IXmlLineInfo info || !info.HasLineInfo() || info.LineNumber > lines.Length)
            return null;
        var index = lines[info.LineNumber - 1] + info.LinePosition - 1 + attribute.Name.LocalName.Length;
        index = SkipSpace(text, index);
        if (index >= text.Length || text[index] != '=')
            return null;
        index = SkipSpace(text, index + 1);
        if (index >= text.Length || text[index] is not ('"' or '\''))
            return null;
        var start = index + 1;
        var end = text.IndexOf(text[index], start);
        if (end < 0)
            return null;
        var line = Array.BinarySearch(lines, start);
        line = line >= 0 ? line : ~line - 1;
        return new FormXamlNameRange(start, end - start, line + 1, start - lines[line] + 1);
    }

    private static int SkipSpace(string text, int index)
    {
        while (index < text.Length && text[index] is ' ' or '\t' or '\r' or '\n')
            index++;
        return index;
    }
}

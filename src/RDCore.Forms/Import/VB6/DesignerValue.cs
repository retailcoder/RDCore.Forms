using System.Globalization;
using RDCore.Forms.Model;
using RDCore.SDK.Runtime.Stdole;

namespace RDCore.Forms.Import.VB6;

// A value a VB6 form's file keeps for a placed UserControl, as text, typed as VB6 types it for the PropertyBag the UserControl's
// ReadProperties reads (measured with VB6).
internal static class DesignerValue
{
    // The class id a designer file writes after the name of a bag that persists a stdole.StdFont.
    private const string StdFontClassId = "{0BE35203-8F91-11CE-9DE3-00AA004BB851}";

    // The value VB6 reads from the text: a quoted string is a String; a number is an Integer where it fits one and is written without
    // a type suffix, a Long where it fits one or is written with &, otherwise a Currency where it fits one, otherwise a Double; a
    // StdFont bag is a StdFont. A record of the companion file and any other text give no value.
    public static bool TryType(string text, out FormValue value)
    {
        ArgumentNullException.ThrowIfNull(text);
        value = default;
        if (text.TrimStart().StartsWith("BeginProperty", StringComparison.OrdinalIgnoreCase))
        {
            return TryFont(text, out value);
        }

        switch (ValueLiteral.Parse(text))
        {
            case ValueLiteral.Text quoted:
                value = FormValue.FromString(quoted.Value);
                return true;
            case ValueLiteral.Number number:
                value = Number(number, Written(text));
                return true;
            default:
                return false;
        }
    }

    // The number's text without its comment, such as -1 for -1 'True.
    private static string Written(string text)
    {
        var comment = text.IndexOf('\'');
        return (comment < 0 ? text : text[..comment]).Trim();
    }

    private static FormValue Number(ValueLiteral.Number number, string written)
    {
        var isLong = written.EndsWith('&');
        if (number.IsIntegral && !isLong && number.Value is >= short.MinValue and <= short.MaxValue)
        {
            return FormValue.FromInteger((short)number.Value);
        }

        if (number.IsIntegral && number.Value is >= int.MinValue and <= int.MaxValue)
        {
            return FormValue.FromLong((int)number.Value);
        }

        return Math.Abs(number.Value) <= 922337203685477.5807 && (decimal)number.Value is var currency && decimal.Round(currency, 4) == currency
            ? FormValue.FromCurrency(currency)
            : FormValue.FromDouble(number.Value);
    }

    // A StdFont bag, read as a form's Font bag is; a bag of another class, or one whose fields do not make a font, gives no value.
    private static bool TryFont(string text, out FormValue value)
    {
        value = default;
        var firstLine = text.AsSpan().TrimStart();
        var lineEnd = firstLine.IndexOfAny('\r', '\n');
        if (!(lineEnd < 0 ? firstLine : firstLine[..lineEnd]).Contains(StdFontClassId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var file = DesignerFile.Parse(string.Create(CultureInfo.InvariantCulture, $"Begin VB.UserControl Bag\r\n{text}\r\nEnd\r\n"),
            new DiagnosticBag());
        if (file.Root?.Bags is not [var bag] || VB6FormReader.ReadFont(bag) is not { } font
            || font.TryToStdFont(out var stdFont) != StdFontFailure.None)
        {
            return false;
        }

        value = FormValue.FromObject(stdFont);
        return true;
    }
}

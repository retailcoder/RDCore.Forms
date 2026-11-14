using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// The rules of SelStart, SelLength and SelText that a TextBox and a ComboBox's edit box share, as measured with VB6.
internal static class TextSelection
{
    // SelStart and SelLength take 0 to 65535, and clamp a value past the text; another value is 380.
    internal static void Check(int value)
    {
        if (value is < 0 or > 65535)
            throw new FormErrorException(FormError.InvalidPropertyValue);
    }

    // What of `value` goes in place of the `length` characters selected in `text`: all of it, or with a MaxLength (0 for none) what
    // fits, which is nothing where the rest of the text is that long already.
    internal static string Fitting(string text, int length, string value, int maxLength)
    {
        if (maxLength == 0)
            return value;
        var room = Math.Max(0, maxLength - (text.Length - length));
        return value.Length > room ? value[..room] : value;
    }
}

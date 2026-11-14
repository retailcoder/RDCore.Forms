using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// A program's selection in a TextBox or in a ComboBox's edit box, with the rules of SelStart, SelLength and SelText, as measured with
// VB6. VB's selection can start or end between the CR and the LF of a new line, where an Avalonia caret does not stop: it is kept here,
// `show` puts it in the box as near as the box allows, and the user's own selection in the box comes back with Take.
internal sealed class TextSelection(Func<string> text, Action<int, int> show)
{
    private int start;
    private int length;

    internal int Start => Math.Min(start, text().Length);

    internal int Length => Math.Min(length, text().Length - Start);

    internal string Selected => text().Substring(Start, Length);

    // SelStart: 0 to 65535, a value past the text being its end; another value is 380. It selects nothing.
    internal void SetStart(int value)
    {
        Check(value);
        Select(Math.Min(value, text().Length), 0);
    }

    // SelLength: 0 to 65535 from SelStart, a value past the text selecting up to its end; another value is 380.
    internal void SetLength(int value)
    {
        Check(value);
        var from = Start;
        Select(from, Math.Min(value, text().Length - from));
    }

    // A program's selection, shown in the box.
    internal void Select(int from, int count)
    {
        (start, length) = (from, count);
        show(from, count);
    }

    // The user's selection in the box, from its anchor to its caret.
    internal void Take(int anchor, int caret) => (start, length) = (Math.Min(anchor, caret), Math.Abs(caret - anchor));

    // The texts SelText puts in the box, in order, each with the caret after it and a Change of its own. With a MaxLength (0 for none),
    // what does not fit is left out. A single-line box takes the value in one step, even where the text does not change, and in none
    // where nothing is selected and nothing of a value that is not "" fits. A multi-line box deletes the selection, where there is
    // one, then inserts what fits, where some does.
    internal IReadOnlyList<(string Text, int Caret)> Replacing(string value, int maxLength, bool multiLine)
    {
        var current = text();
        var (from, count) = (Start, Length);
        var inserted = Fitting(current, count, value, maxLength);
        if (!multiLine)
            return count > 0 || inserted.Length > 0 || value.Length == 0
                ? [(current.Remove(from, count).Insert(from, inserted), from + inserted.Length)]
                : [];
        var steps = new List<(string, int)>();
        if (count > 0)
            steps.Add((current = current.Remove(from, count), from));
        if (inserted.Length > 0)
            steps.Add((current.Insert(from, inserted), from + inserted.Length));
        return steps;
    }

    private static void Check(int value)
    {
        if (value is < 0 or > 65535)
            throw new FormErrorException(FormError.InvalidPropertyValue);
    }

    private static string Fitting(string text, int length, string value, int maxLength)
    {
        if (maxLength == 0)
            return value;
        var room = Math.Max(0, maxLength - (text.Length - length));
        return value.Length > room ? value[..room] : value;
    }
}

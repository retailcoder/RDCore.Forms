namespace RDCore.Forms.Avalonia.Controls;

// What a word typed in a ComboBox matches of its items, as RD-VBA's code completion matches names: an item whose text starts with the word
// first, then one whose humps it matches, without regard to case. A label is cut into humps at a capital after a lowercase letter or a digit,
// at the last capital of a run that a lowercase letter follows, at a run of digits, and after an underscore: "MultiPlayer" is Multi, Player,
// "IOError" is IO, Error, "Win32Api" is Win, 32, Api. The word matches the humps when it can be cut into parts that start the humps from the
// first, in order, none skipped: "MP" and "MuPl" match MultiPlayer, "Pl" does not. An underscore typed ends a part.
internal static class ItemCompletion
{
    public enum Match
    {
        None,
        Humps,
        Prefix,
    }

    // The items the word matches, best first: those it starts, then those whose humps it matches, each in the list's order. None for no word.
    public static List<int> Matches(System.Collections.IList items, string word)
    {
        var prefix = new List<int>();
        var humps = new List<int>();
        if (word.Length == 0)
            return prefix;
        for (var index = 0; index < items.Count; index++)
        {
            switch (items[index] is string text ? MatchOf(text, word) : Match.None)
            {
                case Match.Prefix:
                    prefix.Add(index);
                    break;
                case Match.Humps:
                    humps.Add(index);
                    break;
            }
        }
        prefix.AddRange(humps);
        return prefix;
    }

    public static Match MatchOf(string label, string word)
    {
        if (label.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            return Match.Prefix;

        // The underscores a word starts with are of a label that starts with one, and start no part.
        var first = PastUnderscores(word, 0);
        if (first > 0 && label is not ['_', ..])
            return Match.None;
        if (first == word.Length)
            return Match.Humps;

        // Where a part of the word still to match a hump can start, after the humps walked so far. Walked from the end, so that a start is
        // taken before the starts it leads to are marked for the next hump.
        Span<bool> starts = word.Length <= 128 ? stackalloc bool[word.Length] : new bool[word.Length];
        starts[first] = true;
        var position = 0;
        while (NextHump(label, ref position) is var (hump, length) && length > 0)
        {
            var live = false;
            for (var start = word.Length - 1; start >= 0; start--)
            {
                if (!starts[start])
                    continue;
                starts[start] = false;
                for (var taken = 1; taken <= length && start + taken <= word.Length; taken++)
                {
                    var character = word[start + taken - 1];
                    if (character == '_' || char.ToUpperInvariant(character) != char.ToUpperInvariant(label[hump + taken - 1]))
                        break;
                    var next = PastUnderscores(word, start + taken);
                    if (next == word.Length)
                        return Match.Humps;
                    starts[next] = true;
                    live = true;
                }
            }
            if (!live)
                return Match.None;
        }
        return Match.None;
    }

    private static int PastUnderscores(string text, int index)
    {
        while (index < text.Length && text[index] == '_')
            index++;
        return index;
    }

    // The next hump of a label from the position, as its start and length, the position moved past it; a length of 0 where there is none.
    private static (int Start, int Length) NextHump(string label, ref int position)
    {
        position = PastUnderscores(label, position);
        var start = position;
        if (start == label.Length)
            return (start, 0);
        position++;
        while (position < label.Length && label[position] != '_' && !StartsHump(label, position))
            position++;
        return (start, position - start);
    }

    private static bool StartsHump(string label, int index)
    {
        var character = label[index];
        var before = label[index - 1];
        if (char.IsDigit(character))
            return !char.IsDigit(before);
        if (char.IsDigit(before))
            return true;
        return char.IsUpper(character)
            && (char.IsLower(before) || char.IsUpper(before) && index + 1 < label.Length && char.IsLower(label[index + 1]));
    }
}

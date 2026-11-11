using RDCore.SDK.Model.Symbols.Completion;

namespace RDCore.Forms.Avalonia.Controls;

// What a word typed in a ComboBox matches of its items, as RD-VBA's code completion matches names (CompletionCandidates.MatchOf): an item
// whose text starts with the word first, then one whose humps it matches, without regard to case.
internal static class ItemCompletion
{
    // The items the word matches, best first: those it starts, then those whose humps it matches, each in the list's order. None for no word.
    public static List<int> Matches(System.Collections.IList items, string word)
    {
        var prefix = new List<int>();
        var humps = new List<int>();
        if (word.Length == 0)
            return prefix;
        for (var index = 0; index < items.Count; index++)
        {
            switch (items[index] is string text ? CompletionCandidates.MatchOf(text, word) : CompletionMatch.None)
            {
                case CompletionMatch.Prefix:
                    prefix.Add(index);
                    break;
                case CompletionMatch.Humps:
                    humps.Add(index);
                    break;
            }
        }
        prefix.AddRange(humps);
        return prefix;
    }
}

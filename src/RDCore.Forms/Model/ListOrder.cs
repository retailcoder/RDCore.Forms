using System.Globalization;

namespace RDCore.Forms.Model;

/// <summary>
/// Where a sorted ListBox or ComboBox puts an item, as VB6's do (<see cref="Properties.Sorted"/>).
/// </summary>
public static class ListOrder
{
    /// <summary>
    /// The index an item takes in a sorted list.
    /// </summary>
    /// <param name="items">The list's items, in their order.</param>
    /// <param name="item">The item to add.</param>
    /// <param name="culture">The culture the items are compared in: the user's regional settings, as VB6's list compares them.</param>
    /// <returns>The index from 0 to the number of items.</returns>
    /// <remarks>
    /// The list is searched by halves from the middle item, and the item goes before the first item it is compared to that sorts after it.
    /// It goes in the place of the first item it is compared to that it equals, so an item equal to several does not always go before or
    /// after all of them: VB6 puts <c>APPLE</c> between <c>apple</c> and <c>Apple</c>. The search assumes <paramref name="items"/> are in
    /// order: an item that <c>AddItem</c> put at an index, or that <c>List</c> replaced, stays where it is, and the search passes over it as
    /// VB6's does. Items are compared as <see cref="Compare"/> compares them.
    /// </remarks>
    public static int IndexOf(IReadOnlyList<string> items, string item, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(culture);
        int low = 0, high = items.Count;
        while (low != high)
        {
            var middle = (low + high) / 2;
            var compared = Compare(items[middle], item, culture);
            if (compared == 0)
            {
                return middle;
            }

            if (compared > 0)
            {
                high = middle;
            }
            else
            {
                low = middle + 1;
            }
        }

        return high;
    }

    /// <summary>
    /// Compares two items as a sorted list does, as measured with VB6.
    /// </summary>
    /// <param name="first">The first item.</param>
    /// <param name="second">The second item.</param>
    /// <param name="culture">The culture the items are compared in.</param>
    /// <returns>
    /// Less than 0 where <paramref name="first"/> sorts first, more than 0 where <paramref name="second"/> does, and 0 where they are equal.
    /// </returns>
    /// <remarks>
    /// The items are compared without regard to case, in <paramref name="culture"/>'s order, with their hyphens and apostrophes left out:
    /// <c>10</c> sorts before <c>9</c>, <c>a b</c> before <c>ab</c>, and <c>a-c</c> after <c>ab</c>. Two items that are then equal are
    /// ordered by their hyphens and apostrophes: an item with none sorts first, and otherwise, at the first one that differs, the item whose
    /// mark is further on sorts first, an apostrophe before a hyphen at the same place, and an item with no more marks before one with more.
    /// So <c>coop</c>, <c>coop-</c>, <c>coo-p</c>, <c>co'op</c>, <c>co-op</c>, <c>co--op</c>, <c>c-o-o-p</c> and <c>-coop</c> sort in that
    /// order, and <c>co-op</c> equals <c>CO-OP</c>.
    /// </remarks>
    public static int Compare(string first, string second, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(culture);
        var compared = culture.CompareInfo.Compare(WithoutMarks(first), WithoutMarks(second), CompareOptions.IgnoreCase);
        return compared != 0 ? compared : CompareMarks(first, second);
    }

    private static bool IsMark(char character) => character is '-' or '\'';

    private static string WithoutMarks(string item) => item.Any(IsMark) ? string.Concat(item.Where(character => !IsMark(character))) : item;

    private static int CompareMarks(string first, string second)
    {
        for (int i = NextMark(first, 0), j = NextMark(second, 0); ; i = NextMark(first, i + 1), j = NextMark(second, j + 1))
        {
            if (i < 0 || j < 0)
            {
                return i < 0 ? (j < 0 ? 0 : -1) : 1;
            }

            if (i != j)
            {
                return i > j ? -1 : 1;
            }

            if (first[i] != second[j])
            {
                return first[i] == '\'' ? -1 : 1;
            }
        }
    }

    private static int NextMark(string item, int from)
    {
        for (var index = from; index < item.Length; index++)
        {
            if (IsMark(item[index]))
            {
                return index;
            }
        }

        return -1;
    }
}

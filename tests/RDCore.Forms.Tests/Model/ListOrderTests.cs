using System.Globalization;
using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class ListOrderTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    // As measured with VB6 (en-US), a ListBox, a Dropdown Combo and a Dropdown List agreeing, native and p-code: each AddItem's NewIndex,
    // then the order the fifteen items are left in. The case is ignored, a hyphen sorts as if it were not there, and an item equal to
    // others goes where the search first meets one of them: APPLE between apple and Apple, Zebra before zebra.
    [TestMethod]
    public void ItemsAddedInTurn_GoWhereVB6sSortedListPutsThem()
    {
        string[] added = ["banana", "Apple", "apple", "APPLE", "Éclair", "eclair", "zebra", "10", "9", "_x", "a b", "ab", "a-c", "", "Zebra"];
        var items = new List<string>();
        var indexes = new List<int>();
        foreach (var item in added)
        {
            var index = ListOrder.IndexOf(items, item, English);
            items.Insert(index, item);
            indexes.Add(index);
        }

        CollectionAssert.AreEqual(new[] { 0, 0, 0, 1, 4, 4, 6, 0, 1, 0, 3, 4, 5, 0, 13 }, indexes);
        CollectionAssert.AreEqual(
            new[] { "", "_x", "10", "9", "a b", "ab", "a-c", "apple", "APPLE", "Apple", "banana", "eclair", "Éclair", "Zebra", "zebra" }, items);
    }

    // As measured with VB6 (en-US), native and p-code: each AddItem's NewIndex, then the order. Hyphens and apostrophes are left out, and
    // order the items they leave equal.
    [TestMethod]
    public void ItemsWithHyphensAndApostrophes_GoWhereVB6sSortedListPutsThem()
    {
        string[] added = ["coop", "co-op", "co'op", "CO-OP", "c-o-o-p", "cop", "coo", "co-o", "-coop", "coop-", "co--op", "co op", "coo-p"];
        var items = new List<string>();
        var indexes = new List<int>();
        foreach (var item in added)
        {
            var index = ListOrder.IndexOf(items, item, English);
            items.Insert(index, item);
            indexes.Add(index);
        }

        CollectionAssert.AreEqual(new[] { 0, 1, 1, 2, 4, 5, 0, 1, 7, 3, 7, 0, 5 }, indexes);
        CollectionAssert.AreEqual(
            new[] { "co op", "coo", "co-o", "coop", "coop-", "coo-p", "co'op", "CO-OP", "co-op", "co--op", "c-o-o-p", "-coop", "cop" }, items);
    }

    // As in VB6: an item put out of order (mango, at 0) stays there, and the search passes over it.
    [TestMethod]
    public void AnItemOutOfOrder_StaysWhereItIs_AndTheSearchPassesOverIt()
    {
        string[] items = ["mango", "", "_x", "aaa", "10", "9", "a b", "ab", "a-c", "apple", "APPLE", "Apple", "banana", "eclair", "Éclair",
            "Zebra", "zebra"];

        Assert.AreEqual(12, ListOrder.IndexOf(items, "b", English));
    }

    [TestMethod]
    public void AnEmptyList_TakesTheItemAt0()
    {
        Assert.AreEqual(0, ListOrder.IndexOf([], "a", English));
    }
}

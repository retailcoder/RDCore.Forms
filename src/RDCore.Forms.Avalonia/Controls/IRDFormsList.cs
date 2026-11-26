using Avalonia.Controls;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// A ListBox's or ComboBox's list members, as a program's code calls them: the window runs them through this.
internal interface IRDFormsList
{
    int ListCount { get; }

    int ListIndex { get; set; }

    int NewIndex { get; }

    int TopIndex { get; set; }

    string Text { get; }

    void SetText(string text);

    void AddItem(string item, int? index = null);

    void RemoveItem(int index);

    void Clear();

    string GetList(int index);

    void SetList(int index, string text);

    int GetItemData(int index);

    void SetItemData(int index, int value);

    byte[]? GetItemIcon(int index);

    void SetItemIcon(int index, byte[]? picture);
}

// The item checks that a ListBox's and a ComboBox's List share.
internal static class ListMembers
{
    // The text of the item at the index, or "" outside the items.
    public static string ItemAt(ItemCollection items, int index) =>
        index >= 0 && index < items.Count ? items[index] as string ?? "" : "";

    // An index outside the items: 381.
    public static void Require(int count, int index)
    {
        if (index < 0 || index >= count)
            throw new FormErrorException(FormError.InvalidPropertyArrayIndex);
    }
}

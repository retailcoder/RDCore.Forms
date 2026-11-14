using Avalonia.Controls;
using Avalonia.Input;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Avalonia.Controls;

// The selection of a list whose MultiSelect is Simple or Extended, as VB6's ListBox and FileListBox keep it:
//  - ListIndex is the item with the focus, 0 from the start, and the selection is a set of items apart from it.
//  - Simple toggles the item pressed, whatever the modifiers; its navigation keys (ListClicks.KeyTarget) move only the focus, and
//    Space toggles.
//  - Extended selects the item pressed alone, toggles it with Ctrl, and with Shift selects from the anchor, the last item pressed or
//    reached without Shift. A drag moves the focus, and on an Extended list selects from the anchor too. A navigation key selects the
//    item it reaches alone, or from the anchor with Shift; Space changes nothing.
// The list raises Click for every key this takes, whether or not anything changed.
internal sealed class MultiSelection(ListBox list, ListBoxMultiSelect mode)
{
    // An Extended list's anchor, which a Shift range runs from.
    private int anchor;
    // A Shift press on an Extended list, whose range comes at the release.
    private bool shiftPressed;

    // The focused item, which need not be selected.
    public int Focus { get; set; }

    public int Count => list.Selection.SelectedIndexes.Count;

    public bool IsSelected(int index) => list.Selection.IsSelected(index);

    // New items: the focus and the anchor on the first.
    public void Reset() => Focus = anchor = 0;

    // Fewer items: the focus stays on one of them.
    public void Removed() => Focus = Math.Min(Focus, Math.Max(0, list.ItemCount - 1));

    // Selecting an item adds it and moves the focus to it; clearing it removes it and leaves the focus. Whether anything changed.
    public bool Set(int index, bool selected)
    {
        if (list.Selection.IsSelected(index) == selected)
            return false;
        if (selected)
        {
            list.Selection.Select(index);
            Focus = index;
        }
        else
            list.Selection.Deselect(index);
        return true;
    }

    // A left press, on the item at index, or below the items for -1.
    public void Press(int index, KeyModifiers modifiers, int clickCount)
    {
        // The press is handled on the way down, so Avalonia gives nothing the keyboard focus; its items take it, not the list.
        if (index >= 0)
            list.ContainerFromIndex(index)?.Focus(NavigationMethod.Pointer);
        // Below the items nothing changes, and a double click's second press toggles nothing again.
        if (index < 0 || clickCount > 1)
            return;
        Focus = index;
        shiftPressed = false;
        if (mode == ListBoxMultiSelect.Simple)
            Toggle(index);
        else if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            // The press leaves only the anchor's own state, and the range comes at the release.
            var from = Anchor();
            SelectOnly(list.Selection.IsSelected(from) ? from : -1);
            shiftPressed = true;
        }
        else if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
        {
            Toggle(index);
            anchor = index;
        }
        else
        {
            SelectOnly(index);
            anchor = index;
        }
    }

    // The pointer held down reaches the item at index.
    public void Drag(int index)
    {
        if (index == Focus)
            return;
        Focus = index;
        if (mode == ListBoxMultiSelect.Extended)
            SelectRange(index);
    }

    public void Release()
    {
        if (!shiftPressed)
            return;
        shiftPressed = false;
        SelectRange(Focus);
    }

    // A navigation key or Space; whether the list takes the key. The others do nothing on a multi-select list.
    public bool KeyDown(Key key, KeyModifiers modifiers)
    {
        if (list.ItemCount == 0)
            return false;
        if (key == Key.Space)
        {
            // Simple toggles the focused item, and Extended changes nothing, with Ctrl or without.
            if (mode == ListBoxMultiSelect.Simple && Focus >= 0 && Focus < list.ItemCount)
                Toggle(Focus);
            return true;
        }
        if (ListClicks.KeyTarget(list, key, Focus) is not { } target)
            return false;
        Focus = target;
        // The focused item comes into view and takes the keyboard focus, as with the mouse; not by a directional move, which would
        // have Avalonia select it.
        ListClicks.Reach(list, Focus);
        if (mode == ListBoxMultiSelect.Simple)
            return true;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            SelectRange(Focus);
        else
        {
            // With Ctrl too: Ctrl+Up selects the item alone, as Up does.
            SelectOnly(Focus);
            anchor = Focus;
        }
        return true;
    }

    // From the anchor to the target, the anchor keeping its own state, and nothing else selected.
    private void SelectRange(int target)
    {
        var from = Anchor();
        var anchorSelected = list.Selection.IsSelected(from);
        list.Selection.BeginBatchUpdate();
        try
        {
            list.Selection.Clear();
            for (var i = Math.Min(from, target); i <= Math.Max(from, target); i++)
                if (i != from || anchorSelected)
                    list.Selection.Select(i);
        }
        finally
        {
            list.Selection.EndBatchUpdate();
        }
    }

    private int Anchor() => Math.Clamp(anchor, 0, Math.Max(0, list.ItemCount - 1));

    private void Toggle(int index)
    {
        if (list.Selection.IsSelected(index))
            list.Selection.Deselect(index);
        else
            list.Selection.Select(index);
    }

    // Only the item selected, or none for -1.
    private void SelectOnly(int index)
    {
        list.Selection.BeginBatchUpdate();
        try
        {
            list.Selection.Clear();
            if (index >= 0)
                list.Selection.Select(index);
        }
        finally
        {
            list.Selection.EndBatchUpdate();
        }
    }
}

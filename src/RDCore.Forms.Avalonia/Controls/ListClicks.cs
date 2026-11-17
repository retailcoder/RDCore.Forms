using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// A list whose MultiSelect can be Simple or Extended: its selection where it is, none where it is None.
internal interface IMultiSelectList
{
    MultiSelection? Multi { get; }
}

// The Click of a list, as a ListBox raises it:
//  - A left press on an item selects it at once, the selection follows the pointer over the items while the button is held, and the
//    release raises Click, before MouseUp. A press below the items, and any right or middle press, selects nothing and raises none.
//  - A key that moves the selection raises Click, after its KeyDown and before its KeyUp; a key that can go no further raises none.
//    A ListBox and a FileListBox move as KeyTarget says.
//  - A character typed selects the next item whose text starts with it, without regard to case, and raises Click.
// On a multi-select list the press, the drag and the keys go to its MultiSelection instead, every key it takes raises Click, and a
// character typed does nothing.
internal static class ListClicks
{
    private static readonly ConditionalWeakTable<ListBox, object> pressedOnItem = [];
    private static readonly object marker = new();

    // Before the control's mouse events, so that Click comes before MouseUp; after its key events, so that KeyDown comes first.
    public static void Attach<T>() where T : ListBox
    {
        // Avalonia's ListBox selects on a right press. Handled on the way down, its own press handler never sees it; the mouse events
        // take handled presses too. A left press on a multi-select list is taken here, for the same reason.
        InputElement.PointerPressedEvent.AddClassHandler<T>((list, e) =>
        {
            if (!e.GetCurrentPoint(list).Properties.IsLeftButtonPressed)
                e.Handled = true;
            else if (Multi(list) is { } multi)
            {
                multi.Press(IndexAt(list, e.Source), e.KeyModifiers, e.ClickCount);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        // A multi-select list's keys, on the way down: its focused item would take Space first, and its scroll viewer PageUp and
        // PageDown. A single-select ListBox's and FileListBox's navigation keys too, for the scroll viewer's sake.
        InputElement.KeyDownEvent.AddClassHandler<T>((list, e) =>
        {
            if (Multi(list) is { } multi)
            {
                if (!multi.KeyDown(e.Key, e.KeyModifiers))
                    return;
                e.Handled = true;
                RDFormsControl.Raise(list, Events.Click);
            }
            else if (list is IMultiSelectList)
                KeyMove(list, e);
        }, RoutingStrategies.Tunnel);
        InputElement.PointerPressedEvent.AddClassHandler<T>((list, e) =>
        {
            if (e.GetCurrentPoint(list).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed && IndexAt(list, e.Source) >= 0)
                pressedOnItem.AddOrUpdate(list, marker);
            else
                pressedOnItem.Remove(list);
        }, handledEventsToo: true);
        InputElement.PointerMovedEvent.AddClassHandler<T>((list, e) =>
        {
            if (!pressedOnItem.TryGetValue(list, out _) || !e.GetCurrentPoint(list).Properties.IsLeftButtonPressed)
                return;
            // Over the empty area, or outside the list, the selection stays on the last item reached.
            var index = IndexAt(list, list.InputHitTest(e.GetPosition(list)));
            if (index < 0)
                return;
            if (Multi(list) is { } multi)
                multi.Drag(index);
            else if (index != list.SelectedIndex)
                list.SelectedIndex = index;
        }, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<T>((list, e) =>
        {
            var click = pressedOnItem.TryGetValue(list, out _) && e.InitialPressMouseButton == MouseButton.Left
                        && !ControlEvents.IsSecondPress(list);
            pressedOnItem.Remove(list);
            Multi(list)?.Release();
            if (click)
                RDFormsControl.Raise(list, Events.Click);
        }, handledEventsToo: true);
    }

    // The press now held raises no Click at its release: it was taken for something else, such as opening a folder.
    public static void Forget(ListBox list) => pressedOnItem.Remove(list);

    // After the list's own key handling: Click where the key moved the selection.
    public static void KeyMoved(ListBox list, int before)
    {
        if (list.SelectedIndex != before)
            RDFormsControl.Raise(list, Events.Click);
    }

    // The item a navigation key reaches from an item, as VB6's ListBox and FileListBox move whatever their MultiSelect: Left and Up one
    // back, Right and Down one on, Home and End the first and the last, PageUp and PageDown as many as the list shows whole, less one.
    // null for another key, or for a list with no items.
    public static int? KeyTarget(ListBox list, Key key, int from)
    {
        var last = list.ItemCount - 1;
        if (last < 0)
            return null;
        return key switch
        {
            Key.Left or Key.Up => Math.Max(0, from - 1),
            Key.Right or Key.Down => Math.Min(last, from + 1),
            Key.Home => 0,
            Key.End => last,
            Key.PageUp => Math.Max(0, from - Page(list)),
            Key.PageDown => Math.Min(last, from + Page(list)),
            _ => null,
        };
    }

    // A single-select list's navigation key, before the list's own: the item it reaches is selected, with Click where it is another.
    private static void KeyMove(ListBox list, KeyEventArgs e)
    {
        if (KeyTarget(list, e.Key, list.SelectedIndex) is not { } target)
            return;
        e.Handled = true;
        var moved = target != list.SelectedIndex;
        list.SelectedIndex = target;
        Reach(list, target);
        if (moved)
            RDFormsControl.Raise(list, Events.Click);
    }

    // The item comes into view and takes the keyboard focus, so that the next key moves on from it.
    public static void Reach(ListBox list, int index)
    {
        list.ScrollIntoView(index);
        list.ContainerFromIndex(index)?.Focus(NavigationMethod.Unspecified);
    }

    // As many items as the list shows whole, less one, and at least one.
    private static int Page(ListBox list)
    {
        var viewport = list.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()?.Viewport.Height ?? 0;
        var item = list.GetRealizedContainers().FirstOrDefault()?.Bounds.Height ?? 0;
        return item > 0 ? Math.Max(1, (int)(viewport / item) - 1) : 1;
    }

    // After the list's own text input handling.
    public static void Typed(ListBox list, TextInputEventArgs e)
    {
        if (e.Handled || Multi(list) is not null || string.IsNullOrEmpty(e.Text) || list.ItemCount == 0)
            return;
        e.Handled = true;
        foreach (var character in e.Text)
        {
            if (ListItems.NextStartingWith(list.Items, list.SelectedIndex, character) is not { } index)
                continue;
            list.SelectedIndex = index;
            list.ScrollIntoView(index);
            // The item takes the keyboard focus, so that an arrow moves on from it.
            list.ContainerFromIndex(index)?.Focus(NavigationMethod.Unspecified);
            RDFormsControl.Raise(list, Events.Click);
        }
    }

    // The index of the item a point or an event's source is on, or -1 for the empty area and anything outside the items.
    public static int IndexAt(ListBox list, object? source)
    {
        for (var visual = source as Visual; visual is not null && !ReferenceEquals(visual, list); visual = visual.GetVisualParent())
            if (visual is ListBoxItem item)
                return list.IndexFromContainer(item);
        return -1;
    }

    private static MultiSelection? Multi(ListBox list) => (list as IMultiSelectList)?.Multi;
}

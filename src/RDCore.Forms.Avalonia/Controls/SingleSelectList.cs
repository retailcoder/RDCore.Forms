using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Avalonia.Controls;

// The Click of a list that selects one item, as a ListBox with a MultiSelect of None raises it:
//  - A left press on an item selects it at once, the selection follows the pointer over the items while the button is held, and the
//    release raises Click, before MouseUp. A press below the items, and any right or middle press, selects nothing and raises none.
//  - A key that moves the selection raises Click, after its KeyDown and before its KeyUp; a key that can go no further raises none.
//  - A character typed selects the next item whose text starts with it, without regard to case, and raises Click.
internal static class SingleSelectList
{
    private static readonly ConditionalWeakTable<ListBox, object> pressedOnItem = [];
    private static readonly object marker = new();

    // Before the control's mouse events, so that Click comes before MouseUp; after its key events, so that KeyDown comes first.
    public static void Attach<T>() where T : ListBox
    {
        // Avalonia's ListBox selects on a right press. Handled on the way down, its own press handler never sees it; the mouse events
        // take handled presses too.
        InputElement.PointerPressedEvent.AddClassHandler<T>((list, e) =>
        {
            if (!e.GetCurrentPoint(list).Properties.IsLeftButtonPressed)
                e.Handled = true;
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
            if (index >= 0 && index != list.SelectedIndex)
                list.SelectedIndex = index;
        }, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<T>((list, e) =>
        {
            var click = pressedOnItem.TryGetValue(list, out _) && e.InitialPressMouseButton == MouseButton.Left
                        && !ControlEvents.IsSecondPress(list);
            pressedOnItem.Remove(list);
            if (click)
                VBControl.Raise(list, VBEvents.Click);
        }, handledEventsToo: true);
    }

    // The press now held raises no Click at its release: it was taken for something else, such as opening a folder.
    public static void Forget(ListBox list) => pressedOnItem.Remove(list);

    // After the list's own key handling: Click where the key moved the selection.
    public static void KeyMoved(ListBox list, int before)
    {
        if (list.SelectedIndex != before)
            VBControl.Raise(list, VBEvents.Click);
    }

    // After the list's own text input handling.
    public static void Typed(ListBox list, TextInputEventArgs e)
    {
        if (e.Handled || string.IsNullOrEmpty(e.Text) || list.ItemCount == 0)
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
            VBControl.Raise(list, VBEvents.Click);
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
}

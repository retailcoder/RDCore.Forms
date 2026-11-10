// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB ListBox: a <see cref="ListBox"/> that shows a <see cref="VBClasses.ListBox"/> component.
/// </summary>
/// <remarks>
/// <para>
/// Its items are the component's <c>List</c>. <see cref="AddItem"/>, <see cref="RemoveItem"/> and <see cref="Clear"/> change it, keep
/// its <c>ItemData</c> and <c>ItemIcon</c> in step, and keep the selection on the items that stay. An added item's data is 0.
/// </para>
/// <para>
/// An item's icon, its <c>ItemIcon</c>, is drawn before its text, scaled to fit a square as tall as the text. Where any item has one,
/// every item's text starts after that square, so that the text lines up. A metafile icon is not drawn. Item icons are an RD-VBA
/// addition.
/// </para>
/// <para>
/// Its Click comes from two places, as measured with VB6. A left press on an item raises Click at its release, before MouseUp, whether
/// or not the selection changed and wherever the release lands; the press selects at once, and the selection follows the pointer
/// over the items while the button is held. A left press below the items, and any right or middle press, selects nothing and raises
/// no Click. A key or code that moves the selection raises Click, with <see cref="ListIndex"/> already the new value; setting it
/// again, a key that can go no further, and a change to the items raise none.
/// </para>
/// <para>
/// With a <c>MultiSelect</c> of Simple or Extended, <see cref="ListIndex"/> is the item with the focus, 0 from the start, and the
/// selection is a set of items apart from it. Simple toggles the item pressed, whatever the modifiers; its arrow keys move only the
/// focus, and Space toggles. Extended selects the item pressed alone, toggles it with Ctrl, and with Shift selects from the anchor,
/// the last item pressed or reached without Shift. A drag moves the focus, and on an Extended list selects from the anchor too.
/// Every key such a list takes raises Click, whether or not anything changed.
/// </para>
/// <para>
/// It raises DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp. A key's Click comes after its
/// KeyDown, and its KeyPress where it types a character, and before its KeyUp. Where its <c>MultiSelect</c> is None, a character
/// typed selects the next item that starts with it, after the selected one and from the top again past the last, compared without
/// regard to case, and raises Click where the selection moved.
/// </para>
/// </remarks>
public sealed class VBListBox : ListBox, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private readonly ListBoxMultiSelect multiSelect;
    // Whether the press now held is a left press on an item, which raises Click at its release.
    private bool pressedOnItem;
    // A multi-select list's ListIndex: the focused item, which need not be selected.
    private int focusIndex;
    // An Extended list's anchor, which a Shift range runs from.
    private int anchorIndex;
    // A Shift press on an Extended list, whose range comes at the release.
    private bool shiftPressed;
    // Set while the list writes its own items to the component.
    private bool writingList;
    private readonly ItemIcons icons = new();

    static VBListBox()
    {
        // First, so that a key's KeyDown comes before the Click of the selection it moves.
        ControlKeys.Attach<VBListBox>();
        // VB selects nothing for a right or middle press, where Avalonia's ListBox selects on a right one. Handled on the way down,
        // its own press handler never sees it; the mouse events take handled presses too. A left press on a multi-select list is
        // taken here, for the same reason.
        PointerPressedEvent.AddClassHandler<VBListBox>((list, e) =>
        {
            if (!e.GetCurrentPoint(list).Properties.IsLeftButtonPressed)
                e.Handled = true;
            else if (list.IsMultiSelect)
            {
                list.PressMulti(list.IndexAt(e.Source), e.KeyModifiers, e.ClickCount);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        // A multi-select list's keys, on the way down: its focused item would take Space first.
        KeyDownEvent.AddClassHandler<VBListBox>((list, e) =>
        {
            if (list.IsMultiSelect && list.KeyMulti(e.Key, e.KeyModifiers))
            {
                e.Handled = true;
                VBControl.Raise(list, VBEvents.Click);
            }
        }, RoutingStrategies.Tunnel);
        // Before the mouse events, so that Click comes before MouseUp.
        PointerPressedEvent.AddClassHandler<VBListBox>((list, e) => list.Pressed(e), handledEventsToo: true);
        PointerMovedEvent.AddClassHandler<VBListBox>((list, e) => list.Moved(e), handledEventsToo: true);
        PointerReleasedEvent.AddClassHandler<VBListBox>((list, e) => list.Released(e), handledEventsToo: true);
        ControlEvents.AttachMouse<VBListBox>(DblClick.InPlaceOfMouseDown);
        // A ListBox takes the focus itself, not only through its items.
        FocusableProperty.OverrideDefaultValue<VBListBox>(true);
        ControlEvents.AttachFocus<VBListBox>();
    }

    /// <summary>
    /// Creates a list that shows a ListBox.
    /// </summary>
    /// <param name="instance">The ListBox.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a ListBox.</exception>
    /// <remarks>
    /// Its <c>MultiSelect</c> is read once, here: VB sets it at design time only.
    /// </remarks>
    public VBListBox(ComponentInstance instance)
    {
        this.instance = instance;
        multiSelect = instance?.Get(P.MultiSelect) ?? ListBoxMultiSelect.None;
        if (IsMultiSelect)
            SelectionMode = SelectionMode.Multiple;
        ComponentBinding.Bind(this, instance!, VBClasses.ListBox, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ListBox);

    private bool IsMultiSelect => multiSelect != ListBoxMultiSelect.None;

    /// <summary>
    /// The number of items.
    /// </summary>
    public int ListCount => ItemCount;

    /// <summary>
    /// The selected item, or on a multi-select list the focused one; -1 for none.
    /// </summary>
    /// <exception cref="FormErrorException">Set outside -1 to <see cref="ListCount"/> - 1: 380, Invalid property value.</exception>
    /// <remarks>
    /// Setting it raises Click when it moves the selection, and none when it is set again. On a multi-select list it moves the focus,
    /// and selects and clears nothing.
    /// </remarks>
    public int ListIndex
    {
        get => IsMultiSelect ? focusIndex : SelectedIndex;
        set
        {
            if (value < -1 || value >= ItemCount)
                throw new FormErrorException(FormError.InvalidPropertyValue);
            if (value == ListIndex)
                return;
            if (IsMultiSelect)
                focusIndex = value;
            else
                SelectedIndex = value;
            VBControl.Raise(this, VBEvents.Click);
        }
    }

    /// <summary>
    /// The number of items selected.
    /// </summary>
    public int SelCount => IsMultiSelect ? Selection.SelectedIndexes.Count : SelectedIndex >= 0 ? 1 : 0;

    /// <summary>
    /// The text of the item at <see cref="ListIndex"/>, or "" where there is none.
    /// </summary>
    public string Text => ListIndex >= 0 && ListIndex < ItemCount ? Items[ListIndex] as string ?? "" : "";

    /// <summary>
    /// Gets whether an item is selected.
    /// </summary>
    /// <param name="index">The item's index, from 0.</param>
    /// <returns>Whether it is selected.</returns>
    /// <exception cref="FormErrorException"><paramref name="index"/> is outside the items: 381, Invalid property array index.</exception>
    public bool GetSelected(int index)
    {
        RequireItem(index);
        return IsMultiSelect ? Selection.IsSelected(index) : index == SelectedIndex;
    }

    /// <summary>
    /// Selects or clears an item, and raises Click when that changes something.
    /// </summary>
    /// <param name="index">The item's index, from 0.</param>
    /// <param name="selected">Whether to select it.</param>
    /// <exception cref="FormErrorException"><paramref name="index"/> is outside the items: 381, Invalid property array index.</exception>
    /// <remarks>
    /// On a single-select list it moves the selection. On a multi-select one, selecting an item adds it and moves the focus to it,
    /// and clearing it removes it and leaves the focus.
    /// </remarks>
    public void SetSelected(int index, bool selected)
    {
        RequireItem(index);
        if (!IsMultiSelect)
        {
            if (selected)
                ListIndex = index;
            else if (index == SelectedIndex)
                ListIndex = -1;
            return;
        }
        if (Selection.IsSelected(index) == selected)
            return;
        if (selected)
        {
            Selection.Select(index);
            focusIndex = index;
        }
        else
            Selection.Deselect(index);
        VBControl.Raise(this, VBEvents.Click);
    }

    /// <summary>
    /// Selects the first item whose whole text is <paramref name="text"/>, without regard to case, or none, and raises Click even
    /// when the selection does not move. On a multi-select list it raises Click and changes nothing.
    /// </summary>
    /// <param name="text">The text.</param>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!IsMultiSelect)
        {
            var index = -1;
            for (var i = 0; i < ItemCount && index < 0; i++)
                if (string.Equals(Items[i] as string, text, StringComparison.OrdinalIgnoreCase))
                    index = i;
            SelectedIndex = index;
        }
        VBControl.Raise(this, VBEvents.Click);
    }

    /// <summary>
    /// Adds an item.
    /// </summary>
    /// <param name="item">The item's text.</param>
    /// <param name="index">Where it goes, from 0; at the end where it is <see langword="null"/>.</param>
    /// <exception cref="FormErrorException"><paramref name="index"/> is outside 0 to <see cref="ListCount"/>: 5, Invalid procedure call or argument.</exception>
    public void AddItem(string item, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(item);
        var at = index ?? ItemCount;
        if (at < 0 || at > ItemCount)
            throw new FormErrorException(FormError.InvalidProcedureCall);
        Items.Insert(at, item);
        WriteList();
        ListItems.Inserted(instance, ItemCount - 1, at);
    }

    /// <summary>
    /// Removes an item.
    /// </summary>
    /// <param name="index">The item's index, from 0.</param>
    /// <exception cref="FormErrorException"><paramref name="index"/> is outside the items: 5, Invalid procedure call or argument.</exception>
    public void RemoveItem(int index)
    {
        if (index < 0 || index >= ItemCount)
            throw new FormErrorException(FormError.InvalidProcedureCall);
        Items.RemoveAt(index);
        if (focusIndex >= ItemCount)
            focusIndex = Math.Max(0, ItemCount - 1);
        WriteList();
        ListItems.Removed(instance, ItemCount + 1, index);
    }

    /// <summary>
    /// Removes every item.
    /// </summary>
    public void Clear()
    {
        Items.Clear();
        focusIndex = anchorIndex = 0;
        WriteList();
        ListItems.Cleared(instance);
    }

    /// <summary>
    /// Gets an item's data, the Long value attached to it.
    /// </summary>
    /// <param name="index">The item's index, from 0.</param>
    /// <returns>The item's data.</returns>
    /// <exception cref="FormErrorException"><paramref name="index"/> is outside the items: 381, Invalid property array index.</exception>
    public int GetItemData(int index) => ListItems.GetData(instance, ItemCount, index);

    /// <summary>
    /// Sets an item's data, the Long value attached to it.
    /// </summary>
    /// <param name="index">The item's index, from 0.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="FormErrorException"><paramref name="index"/> is outside the items: 381, Invalid property array index.</exception>
    public void SetItemData(int index, int value) => ListItems.SetData(instance, ItemCount, index, value);

    /// <summary>
    /// Gets an item's icon, the picture drawn before its text. Item icons are an RD-VBA addition.
    /// </summary>
    /// <param name="index">The item's index, from 0.</param>
    /// <returns>The picture, as the bytes of its file, or <see langword="null"/> where the item has none.</returns>
    /// <exception cref="FormErrorException"><paramref name="index"/> is outside the items: 381, Invalid property array index.</exception>
    public byte[]? GetItemIcon(int index) => ListItems.GetIcon(instance, ItemCount, index);

    /// <summary>
    /// Sets an item's icon, the picture drawn before its text. Item icons are an RD-VBA addition.
    /// </summary>
    /// <param name="index">The item's index, from 0.</param>
    /// <param name="picture">The picture, as the bytes of its file; <see langword="null"/> or no bytes for none.</param>
    /// <exception cref="FormErrorException">
    /// <paramref name="index"/> is outside the items: 381, Invalid property array index. <paramref name="picture"/> is refused by
    /// <see cref="FormPictures.Inspect"/>: 481, Invalid picture.
    /// </exception>
    public void SetItemIcon(int index, byte[]? picture) => ListItems.SetIcon(instance, ItemCount, index, picture);

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // A multi-select list's keys are taken on the way down; Avalonia's own would select as Avalonia does.
        if (IsMultiSelect)
            return;
        var before = SelectedIndex;
        base.OnKeyDown(e);
        // An arrow that moves the selection raises Click; one that can go no further does not.
        if (SelectedIndex != before)
            VBControl.Raise(this, VBEvents.Click);
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (e.Handled || IsMultiSelect || string.IsNullOrEmpty(e.Text) || ItemCount == 0)
            return;
        e.Handled = true;
        foreach (var character in e.Text)
            Search(character);
    }

    // The item a character selects takes the keyboard focus, so that an arrow moves on from it.
    private void Search(char character)
    {
        if (ListItems.NextStartingWith(Items, SelectedIndex, character) is not { } index)
            return;
        SelectedIndex = index;
        ScrollIntoView(index);
        ContainerFromIndex(index)?.Focus(NavigationMethod.Unspecified);
        VBControl.Raise(this, VBEvents.Click);
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.List))
        {
            if (writingList)
                return;
            Items.Clear();
            foreach (var item in (IReadOnlyList<string>)e.After!)
                Items.Add(item);
            focusIndex = anchorIndex = 0;
        }
        else if (ReferenceEquals(e.Property, P.ItemIcon))
        {
            icons.Take((IReadOnlyList<byte[]?>)e.After!);
            icons.ShowAll(this);
        }
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        icons.Show(container, item, index);
    }

    private void WriteList()
    {
        writingList = true;
        try
        {
            instance.Set(P.List, [.. Items.Cast<string>()]);
        }
        finally
        {
            writingList = false;
        }
    }

    private void RequireItem(int index)
    {
        if (index < 0 || index >= ItemCount)
            throw new FormErrorException(FormError.InvalidPropertyArrayIndex);
    }

    private void Pressed(PointerPressedEventArgs e) =>
        pressedOnItem = e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed && IndexAt(e.Source) >= 0;

    private void PressMulti(int index, KeyModifiers modifiers, int clickCount)
    {
        // The press is handled here, so Avalonia gives nothing the keyboard focus; its items take it, not the list.
        if (index >= 0)
            ContainerFromIndex(index)?.Focus(NavigationMethod.Pointer);
        // Below the items nothing changes, and a double click's second press toggles nothing again.
        if (index < 0 || clickCount > 1)
            return;
        focusIndex = index;
        shiftPressed = false;
        if (multiSelect == ListBoxMultiSelect.Simple)
            Toggle(index);
        else if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            // The press leaves only the anchor's own state, and the range comes at the release.
            var anchor = Anchor();
            SelectOnly(Selection.IsSelected(anchor) ? anchor : -1);
            shiftPressed = true;
        }
        else if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
        {
            Toggle(index);
            anchorIndex = index;
        }
        else
        {
            SelectOnly(index);
            anchorIndex = index;
        }
    }

    private void Moved(PointerEventArgs e)
    {
        if (!pressedOnItem || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        // Over the empty area, or outside the list, the selection stays on the last item reached.
        var index = IndexAt(this.InputHitTest(e.GetPosition(this)));
        if (index < 0)
            return;
        if (IsMultiSelect)
        {
            if (index == focusIndex)
                return;
            focusIndex = index;
            if (multiSelect == ListBoxMultiSelect.Extended)
                SelectRange(index);
        }
        else if (index != SelectedIndex)
            SelectedIndex = index;
    }

    private void Released(PointerReleasedEventArgs e)
    {
        var click = pressedOnItem && e.InitialPressMouseButton == MouseButton.Left && !ControlEvents.IsSecondPress(this);
        pressedOnItem = false;
        if (shiftPressed)
        {
            shiftPressed = false;
            SelectRange(focusIndex);
        }
        if (click)
            VBControl.Raise(this, VBEvents.Click);
    }

    // Up, Down and Space, the keys measured. The others do nothing on a multi-select list.
    private bool KeyMulti(Key key, KeyModifiers modifiers)
    {
        if (key is not (Key.Up or Key.Down or Key.Space) || ItemCount == 0)
            return false;
        if (key == Key.Space)
        {
            // Simple toggles the focused item, and Extended changes nothing, with Ctrl or without.
            if (multiSelect == ListBoxMultiSelect.Simple && focusIndex >= 0 && focusIndex < ItemCount)
                Toggle(focusIndex);
            return true;
        }
        focusIndex = Math.Clamp(focusIndex + (key == Key.Down ? 1 : -1), 0, ItemCount - 1);
        // The focused item comes into view and takes the keyboard focus, as with the mouse; not by a directional move, which would
        // have Avalonia select it.
        ScrollIntoView(focusIndex);
        ContainerFromIndex(focusIndex)?.Focus(NavigationMethod.Unspecified);
        if (multiSelect == ListBoxMultiSelect.Simple)
            return true;
        if (modifiers.HasFlag(KeyModifiers.Shift))
            SelectRange(focusIndex);
        else
        {
            // With Ctrl too: Ctrl+Up selects the item alone, as Up does.
            SelectOnly(focusIndex);
            anchorIndex = focusIndex;
        }
        return true;
    }

    // From the anchor to the target, the anchor keeping its own state, and nothing else selected.
    private void SelectRange(int target)
    {
        var anchor = Anchor();
        var anchorSelected = Selection.IsSelected(anchor);
        Selection.BeginBatchUpdate();
        try
        {
            Selection.Clear();
            for (var i = Math.Min(anchor, target); i <= Math.Max(anchor, target); i++)
                if (i != anchor || anchorSelected)
                    Selection.Select(i);
        }
        finally
        {
            Selection.EndBatchUpdate();
        }
    }

    private int Anchor() => Math.Clamp(anchorIndex, 0, Math.Max(0, ItemCount - 1));

    private void Toggle(int index)
    {
        if (Selection.IsSelected(index))
            Selection.Deselect(index);
        else
            Selection.Select(index);
    }

    // Only the item selected, or none for -1.
    private void SelectOnly(int index)
    {
        Selection.BeginBatchUpdate();
        try
        {
            Selection.Clear();
            if (index >= 0)
                Selection.Select(index);
        }
        finally
        {
            Selection.EndBatchUpdate();
        }
    }

    // The index of the item a point or an event's source is on, or -1 for the empty area and anything outside the items.
    private int IndexAt(object? source)
    {
        for (var visual = source as Visual; visual is not null && !ReferenceEquals(visual, this); visual = visual.GetVisualParent())
            if (visual is ListBoxItem item)
                return IndexFromContainer(item);
        return -1;
    }
}

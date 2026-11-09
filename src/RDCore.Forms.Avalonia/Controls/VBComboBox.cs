// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB ComboBox: a <see cref="ComboBox"/> that shows a <see cref="VBClasses.ComboBox"/> component.
/// </summary>
/// <remarks>
/// <para>
/// Its items are the component's <c>List</c>. At a <c>Style</c> of Dropdown Combo, the component's <c>Text</c> is the edit box's:
/// typing sets it, code's <c>Text</c> selects no item, and choosing an item puts its text in the box. At Dropdown List it has no
/// edit box, and <see cref="Text"/> is its selected item's.
/// </para>
/// <para>
/// Its events, as measured with VB6: Click is a choice, raised for every pick of an item by the mouse (the one already selected too),
/// every arrow key that moves the selection with the list open or closed, a Dropdown List's letter key, and code's
/// <see cref="ListIndex"/> when it moves. Change is the edit box's, raised from the component's <c>Text</c>, from typing and from
/// code, never from choosing an item. DropDown is raised as the list opens. It raises GotFocus and LostFocus once as the combo is
/// entered and left, whether the combo or its edit box has the focus.
/// </para>
/// <para>
/// <see cref="AddItem"/>, <see cref="RemoveItem"/> and <see cref="Clear"/> keep the component's <c>ItemData</c> and <c>ItemIcon</c>
/// in step with its items. An added item's data is 0.
/// </para>
/// <para>
/// An item's icon, its <c>ItemIcon</c>, is drawn before its text in the list, scaled to fit a square as tall as the text. Where any
/// item has one, every item's text starts after that square, so that the text lines up. A Dropdown List draws its selected item's icon
/// in its box too; a Dropdown Combo's edit box shows text only. Item icons are an RD-VBA addition.
/// </para>
/// <para>
/// A <c>Style</c> of Simple Combo is shown as a Dropdown Combo, and <c>Locked</c> is not applied.
/// </para>
/// </remarks>
public sealed class VBComboBox : ComboBox
{
    // Avalonia's editable combo selects the first item its Text matches, without regard to case, whether code or typing put the Text
    // there; VB's selects none. So every item reads as this, which no Text can be, and none matches; but not while Avalonia puts the
    // selected item's own text in the box.
    private static readonly string noItemText = $"no item {Guid.NewGuid():N}";
    private static readonly Binding noItemMatches = new(".") { Converter = new FuncValueConverter<object?, string>(_ => noItemText) };

    private readonly ComponentInstance instance;
    private readonly ComboBoxStyle style;
    // Set while the box takes the selected item's text, or the component's.
    private bool textFromSelection;
    private bool textFromComponent;
    private bool writingList;
    private TextBox? editBox;
    private readonly ItemIcons icons = new();

    static VBComboBox()
    {
        ControlEvents.AttachFocus<VBComboBox>();
    }

    /// <summary>
    /// Creates a combo that shows a ComboBox.
    /// </summary>
    /// <param name="instance">The ComboBox.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a ComboBox.</exception>
    /// <remarks>
    /// Its <c>Style</c> is read once, here: VB sets it at design time only.
    /// </remarks>
    public VBComboBox(ComponentInstance instance)
    {
        this.instance = instance;
        style = instance?.Get(P.ComboBoxStyle) ?? ComboBoxStyle.DropdownCombo;
        IsEditable = style != ComboBoxStyle.DropdownList;
        // A Dropdown List shows its item in the combo's own padding, too deep for a combo's height.
        Padding = new Thickness(2, 0);
        VerticalContentAlignment = VerticalAlignment.Center;
        if (IsEditable)
            TextSearch.SetTextBinding(this, noItemMatches);
        // On the way down, so that the edit box does not take them first.
        AddHandler(KeyDownEvent, TakeKey, RoutingStrategies.Tunnel);
        ComponentBinding.Bind(this, instance!, VBClasses.ComboBox, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ComboBox);

    /// <summary>
    /// The number of items.
    /// </summary>
    public int ListCount => ItemCount;

    /// <summary>
    /// The selected item; -1 for none.
    /// </summary>
    /// <exception cref="FormErrorException">Set outside -1 to <see cref="ListCount"/> - 1: 380, Invalid property value.</exception>
    /// <remarks>
    /// Setting it raises Click when it moves the selection, and none when it does not.
    /// </remarks>
    public int ListIndex
    {
        get => SelectedIndex;
        set
        {
            if (value < -1 || value >= ItemCount)
                throw new FormErrorException(FormError.InvalidPropertyValue);
            if (value == SelectedIndex)
                return;
            SelectedIndex = value;
            VBControl.Raise(this, VBEvents.Click);
        }
    }

    /// <summary>
    /// The combo's text: a Dropdown List's selected item, or "" with none; the edit box's otherwise.
    /// </summary>
    public new string Text => style == ComboBoxStyle.DropdownList
        ? SelectedIndex >= 0 && SelectedIndex < ItemCount ? Items[SelectedIndex] as string ?? "" : ""
        : base.Text ?? "";

    /// <summary>
    /// Sets the combo's text, as code's <c>Text</c> does.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <exception cref="FormErrorException">
    /// On a Dropdown List, no item's whole text is <paramref name="text"/>: 383, with VB's description "'Text' property is read-only".
    /// </exception>
    /// <remarks>
    /// On a Dropdown List it selects the first item whose whole text it is, without regard to case, and raises Click every time, even
    /// where that item is selected already. Otherwise the component's <c>Text</c> takes it, the selection clears even where it is an
    /// item's text, and Change is raised, once; nothing happens where it is the text already, compared exactly.
    /// </remarks>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (style == ComboBoxStyle.DropdownList)
        {
            var index = -1;
            for (var i = 0; i < ItemCount && index < 0; i++)
                if (string.Equals(Items[i] as string, text, StringComparison.OrdinalIgnoreCase))
                    index = i;
            if (index < 0)
                throw new FormErrorException(new FormError(383, "'Text' property is read-only"));
            SelectedIndex = index;
            VBControl.Raise(this, VBEvents.Click);
            return;
        }
        if (string.Equals(base.Text ?? "", text, StringComparison.Ordinal))
            return;
        SelectedIndex = -1;
        instance.Set(P.Text, text);
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
        WriteList();
        ListItems.Removed(instance, ItemCount + 1, index);
    }

    /// <summary>
    /// Removes every item, as VB's <c>Clear</c> does.
    /// </summary>
    public new void Clear()
    {
        Items.Clear();
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
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        // The edit box is one line, at a combo's height, too short for a TextBox's own padding.
        editBox = e.NameScope.Find<TextBox>("PART_EditableTextBox");
        if (editBox is not { } box)
            return;
        box.MinHeight = 0;
        box.Padding = new Thickness(2, 0);
        box.VerticalContentAlignment = VerticalAlignment.Center;
        ScrollViewer.SetVerticalScrollBarVisibility(box, ScrollBarVisibility.Disabled);
        ScrollViewer.SetHorizontalScrollBarVisibility(box, ScrollBarVisibility.Hidden);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == SelectedItemProperty && IsEditable && !textFromSelection)
        {
            // The selected item's own text goes in the box, so the guard is off while Avalonia reads it.
            var chosen = SelectedIndex;
            textFromSelection = true;
            TextSearch.SetTextBinding(this, null);
            try
            {
                base.OnPropertyChanged(change);
                // Avalonia matched that text against every item without regard to case, and selected the first match: for an item
                // whose text another shares, the wrong one.
                if (SelectedIndex != chosen)
                    SelectedIndex = chosen;
                // A choice is no Change: the component takes the item's text quietly.
                instance.Set(P.Text, base.Text ?? "");
            }
            finally
            {
                TextSearch.SetTextBinding(this, noItemMatches);
                textFromSelection = false;
            }
            return;
        }
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty && IsEditable && !textFromSelection && !textFromComponent)
            instance.Set(P.Text, base.Text ?? "");
        else if (change.Property == IsDropDownOpenProperty && change.GetNewValue<bool>())
            VBControl.Raise(this, VBEvents.DropDown);
        if (change.Property == SelectedIndexProperty && icons.Any)
            ShowSelectedIcon();
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        icons.Show(container, item, index);
    }

    // A Dropdown List's box shows the selected item's icon. A new template each time, so that the box is drawn again: two items of the
    // same text are the same box item.
    private void ShowSelectedIcon() =>
        SelectionBoxItemTemplate = style == ComboBoxStyle.DropdownList && icons.Any
            ? new FuncDataTemplate<object?>((item, _) => new ItemView(item as string ?? "", icons.At(SelectedIndex)))
            : null;

    /// <inheritdoc/>
    public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
    {
        if (!base.UpdateSelectionFromEvent(container, eventArgs))
            return false;
        // An item picked with the mouse is Click, even the one already selected.
        if (eventArgs is PointerEventArgs)
            Picked();
        return true;
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        var before = SelectedIndex;
        base.OnTextInput(e);
        // A Dropdown List takes a letter as a pick of the first item starting with it.
        if (style == ComboBoxStyle.DropdownList && SelectedIndex != before)
            Picked();
    }

    // Up and Down move the selection at once, list open or closed, and raise Click when they move it; Enter in the open list only
    // closes it. Avalonia moves only the focus in an open list, and from no selection Up selects nothing, where VB's selects the
    // first item.
    private void TakeKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) || ItemCount == 0)
            return;
        if (e.Key == Key.Enter && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            e.Handled = true;
            return;
        }
        if (e.Key is not (Key.Up or Key.Down))
            return;
        e.Handled = true;
        var index = SelectedIndex < 0 ? 0 : Math.Clamp(SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, ItemCount - 1);
        if (index == SelectedIndex)
            return;
        SelectedIndex = index;
        if (IsDropDownOpen)
            ContainerFromIndex(index)?.Focus(NavigationMethod.Directional);
        Picked();
    }

    // A choice puts the item's text in the edit box selected, so that a key typed next replaces it.
    private void Picked()
    {
        if (IsEditable)
            editBox?.SelectAll();
        VBControl.Raise(this, VBEvents.Click);
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.Text))
        {
            var text = (string)e.After!;
            if (IsEditable && !textFromSelection && (base.Text ?? "") != text)
            {
                textFromComponent = true;
                try
                {
                    base.Text = text;
                }
                finally
                {
                    textFromComponent = false;
                }
            }
            if (IsEditable && !textFromSelection && !Equals(e.Before, e.After))
                VBControl.Raise(this, VBEvents.Change);
        }
        else if (ReferenceEquals(e.Property, P.List))
        {
            if (writingList)
                return;
            Items.Clear();
            foreach (var item in (IReadOnlyList<string>)e.After!)
                Items.Add(item);
        }
        else if (ReferenceEquals(e.Property, P.ItemIcon))
        {
            icons.Take((IReadOnlyList<byte[]?>)e.After!);
            icons.ShowAll(this);
            ShowSelectedIcon();
        }
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
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
}

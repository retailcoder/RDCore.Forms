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
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
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
/// every arrow key that moves the selection with the list open or closed, a Dropdown List's character that selects another item,
/// and code's <see cref="ListIndex"/> when it moves. Change is the edit box's, raised from the component's <c>Text</c>, from typing and from
/// code, never from choosing an item. DropDown is raised as the list opens. It raises GotFocus and LostFocus once as the combo is
/// entered and left, whether the combo or its edit box has the focus. KeyDown, KeyPress and KeyUp are raised for the keys of the combo,
/// of its edit box and of its open list, and the form raises none of its own for them where its <c>KeyPreview</c> is False. A key's
/// Click, Change or DropDown comes after its KeyDown, and its KeyPress where it types a character, and before its KeyUp.
/// </para>
/// <para>
/// <see cref="AddItem"/>, <see cref="RemoveItem"/> and <see cref="Clear"/> keep the component's <c>ItemData</c> and <c>ItemIcon</c>
/// in step with its items. An added item's data is 0.
/// </para>
/// <para>
/// An item's icon, its <c>ItemIcon</c>, is drawn before its text in the list, scaled to fit a square as tall as the text. Where any
/// item has one, every item's text starts after that square, so that the text lines up. A Dropdown List draws its selected item's icon
/// in its box too; a Dropdown Combo's edit box shows text only. A metafile icon is not drawn. Item icons are an RD-VBA addition.
/// </para>
/// <para>
/// Typing completes an item's text as RD-VBA's code completion completes a name (an RD-VBA addition): an item whose text starts with
/// what is typed matches first, then one whose humps it matches (<c>MP</c> matches <c>MultiPlayer</c>), without regard to case. A
/// Dropdown List adds up the characters typed into a word while an item matches it, and selects the best match; a character that no item matches with the
/// word starts a new one, which selects the next item it matches after the selected one. A Dropdown Combo's typing opens its list and
/// shows only the items its text matches, best first and highlighted: Up and Down move the highlight, and Enter chooses the
/// highlighted item, as a click on an item does. The text stays what was typed until an item is chosen, and the list closes where no
/// item matches.
/// </para>
/// <para>
/// A Simple Combo shows its edit box, 21 pixels high, with its list always shown under it and no button; the list takes the rest of
/// its height, and scrolls where its items do not fit. A press on the list picks the item under it, or the last item when the
/// press is below the items, even the item already selected; a pick raises Click and no Change, and leaves the focus in the edit box.
/// A double click is one Click, then DblClick. Its list never drops, and it raises no DropDown. Typing does not complete or filter.
/// </para>
/// <para>
/// A <c>Locked</c> combo keeps out the user, not code: its edit box takes no typing, its list does not drop, and neither the keys nor
/// the pointer pick an item. Its key events are raised. Code's <c>Text</c> and <see cref="ListIndex"/> still set it.
/// </para>
/// </remarks>
public sealed class VBComboBox : ComboBox, IRaisesKeyEvents
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
    private bool textFromTyping;
    // A Dropdown List's typing: the word its characters add up, and set while it selects an item.
    private string word = "";
    private bool searching;
    // A Dropdown Combo's typing: the items its text matches, best first, while they narrow its open list, and the highlighted one's place.
    private List<int>? narrowed;
    private int highlighted;
    private CompletionPanel? panel;
    private bool writingList;
    private TextBox? editBox;
    private Border? listBorder;
    private bool locked;
    private readonly ItemIcons icons = new();
    // The selection a program sees in the edit box; set while it goes to the box, and while SelText writes the component's text.
    private readonly TextSelection selection;
    private bool selecting;
    private bool replacing;

    static VBComboBox()
    {
        ControlKeys.Attach<VBComboBox>();
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
        selection = new(() => base.Text ?? "", ShowSelection);
        style = instance?.Get(P.ComboBoxStyle) ?? ComboBoxStyle.DropdownCombo;
        IsEditable = style != ComboBoxStyle.DropdownList;
        if (style == ComboBoxStyle.SimpleCombo)
        {
            Template = SimpleTemplate;
            // VB6's rows are a line of text high.
            Styles.Add(new Style(x => x.OfType<ComboBoxItem>())
            {
                Setters = { new Setter(TemplatedControl.PaddingProperty, new Thickness(2, 0)), new Setter(MinHeightProperty, 0d) },
            });
            // On the way down, so that the press is the list's before an item takes it.
            AddHandler(PointerPressedEvent, PressList, RoutingStrategies.Tunnel);
            AddHandler(PointerReleasedEvent, ReleaseList, RoutingStrategies.Tunnel);
        }
        // A Dropdown List shows its item in the combo's own padding, too deep for a combo's height.
        Padding = new Thickness(2, 0);
        VerticalContentAlignment = VerticalAlignment.Center;
        if (IsEditable)
            TextSearch.SetTextBinding(this, noItemMatches);
        if (style == ComboBoxStyle.DropdownCombo)
            ItemsPanel = new FuncTemplate<Panel?>(() => panel = new CompletionPanel(this));
        // On the way down, so that the edit box does not take them first.
        AddHandler(KeyDownEvent, TakeKey, RoutingStrategies.Tunnel);
        ComponentBinding.Bind(this, instance!, VBClasses.ComboBox, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ComboBox);

    // The edit box's height at VB6's default font, MS Sans Serif 8, where VB6 draws it 21 pixels high.
    private const double SimpleEditHeight = 21;

    // A Simple Combo: its edit box, with its list under it, always shown, and no button. The list scrolls when its items do not fit.
    // Avalonia's ComboBox requires a popup, which is there and never opens.
    private static readonly FuncControlTemplate<VBComboBox> SimpleTemplate = new((combo, scope) =>
    {
        var edit = new TextBox
        {
            Name = "PART_EditableTextBox",
            Height = SimpleEditHeight,
            [DockPanel.DockProperty] = Dock.Top,
            [!!TextBox.TextProperty] = combo[!!TextProperty],
            [!TextBox.BackgroundProperty] = combo[!BackgroundProperty],
            [!TextBox.ForegroundProperty] = combo[!ForegroundProperty],
        }.RegisterInNameScope(scope);
        var items = new ItemsPresenter
        {
            Name = "PART_ItemsPresenter",
            [!ItemsPresenter.ItemsPanelProperty] = combo[!ItemsPanelProperty],
        }.RegisterInNameScope(scope);
        var popup = new Popup { Name = "PART_Popup", [DockPanel.DockProperty] = Dock.Top }.RegisterInNameScope(scope);
        var list = new Border
        {
            Name = "PART_List",
            BorderThickness = new Thickness(1),
            [!Border.BorderBrushProperty] = combo[!BorderBrushProperty],
            [!Border.BackgroundProperty] = combo[!BackgroundProperty],
            Child = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = items,
            },
        }.RegisterInNameScope(scope);
        return new DockPanel { LastChildFill = true, Children = { edit, popup, list } };
    });

    // A press on a Simple Combo's list picks the item under it, or the last item when the press is below the items, even the item
    // already selected, with Click. A double click's second press raises DblClick and picks nothing more. A Locked combo's list picks nothing.
    private void PressList(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || e.Source is not Visual source || !IsInList(source))
            return;
        e.Handled = true;
        if (locked || ItemCount == 0)
        {
            editBox?.Focus(NavigationMethod.Pointer);
            return;
        }
        var item = source as ComboBoxItem ?? source.FindAncestorOfType<ComboBoxItem>();
        if (item is not null && e.ClickCount == 2)
        {
            VBControl.Raise(this, VBEvents.DblClick);
            return;
        }
        SelectedIndex = item is null ? ItemCount - 1 : IndexFromContainer(item);
        Picked();
    }

    // Its release is the press's: the list's own release would pick the item a second time.
    private void ReleaseList(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Source is Visual source && IsInList(source))
            e.Handled = true;
    }

    private bool IsInList(Visual source) =>
        listBorder is { } list && (ReferenceEquals(source, list) || list.IsVisualAncestorOf(source));

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
    /// Where the edit box's selection starts, or its caret's place where nothing is selected: 0 is before the first character.
    /// </summary>
    /// <exception cref="FormErrorException">
    /// Set below 0 or above 65535, or read or set on a Dropdown List, which has no edit box: 380, Invalid property value.
    /// </exception>
    /// <remarks>
    /// As a single-line TextBox's <c>SelStart</c>. A text from code, or an item chosen with <see cref="ListIndex"/>, leaves nothing
    /// selected, the caret before the first character.
    /// </remarks>
    public int SelStart
    {
        get => EditSelection().Start;
        set => EditSelection().SetStart(value);
    }

    /// <summary>
    /// The number of characters selected in the edit box, from <see cref="SelStart"/>.
    /// </summary>
    /// <exception cref="FormErrorException">
    /// Set below 0 or above 65535, or read or set on a Dropdown List: 380, Invalid property value.
    /// </exception>
    /// <remarks>
    /// As a single-line TextBox's <c>SelLength</c>.
    /// </remarks>
    public int SelLength
    {
        get => EditSelection().Length;
        set => EditSelection().SetLength(value);
    }

    /// <summary>
    /// The text selected in the edit box; "" where nothing is selected.
    /// </summary>
    /// <exception cref="ArgumentNullException">Set to <see langword="null"/>.</exception>
    /// <exception cref="FormErrorException">Read or set on a Dropdown List: 380, Invalid property value.</exception>
    /// <remarks>
    /// As a single-line TextBox's <c>SelText</c>: setting it puts the text in place of the selection, the caret after it, and raises
    /// Change once, even where the text does not change. Where an item is selected, it selects none, <see cref="ListIndex"/> -1.
    /// </remarks>
    public string SelText
    {
        get => EditSelection().Selected;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            foreach (var (text, caret) in EditSelection().Replacing(value, 0, multiLine: false))
                Replace(text, caret);
        }
    }

    // A Dropdown List has no edit box, and no selection to read or set.
    private TextSelection EditSelection() =>
        style == ComboBoxStyle.DropdownList ? throw new FormErrorException(FormError.InvalidPropertyValue) : selection;

    // The caret goes to the selection's end first: setting it empties the selection. A combo whose template is not applied yet has no
    // edit box: the selection goes to it when it has one.
    private void ShowSelection(int start, int length)
    {
        if (editBox is not { } box)
            return;
        selecting = true;
        try
        {
            box.CaretIndex = start + length;
            box.SelectionStart = start;
            box.SelectionEnd = start + length;
        }
        finally
        {
            selecting = false;
        }
    }

    // The user's selection in the edit box.
    private void TakeSelection(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (!selecting && sender is TextBox box
            && (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty || e.Property == TextBox.CaretIndexProperty))
            selection.Take(box.SelectionStart, box.SelectionEnd);
    }

    // The component takes the text, the caret goes to its place, and the combo raises its Change: one, whether or not the text changed.
    // An item selected is selected no more.
    private void Replace(string text, int caret)
    {
        replacing = true;
        try
        {
            if (SelectedIndex >= 0)
                SelectedIndex = -1;
            instance.Set(P.Text, text);
        }
        finally
        {
            replacing = false;
        }
        selection.Select(caret, 0);
        VBControl.Raise(this, VBEvents.Change);
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
        if (editBox is { } old)
            old.PropertyChanged -= TakeSelection;
        // The edit box is one line, at a combo's height, too short for a TextBox's own padding.
        editBox = e.NameScope.Find<TextBox>("PART_EditableTextBox");
        listBorder = e.NameScope.Find<Border>("PART_List");
        if (editBox is not { } box)
            return;
        box.PropertyChanged += TakeSelection;
        ShowSelection(selection.Start, selection.Length);
        box.IsReadOnly = locked;
        box.MinHeight = 0;
        box.Padding = new Thickness(2, 0);
        box.VerticalContentAlignment = VerticalAlignment.Center;
        ScrollViewer.SetVerticalScrollBarVisibility(box, ScrollBarVisibility.Disabled);
        ScrollViewer.SetHorizontalScrollBarVisibility(box, ScrollBarVisibility.Hidden);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        // Typing clears the selection, and the box keeps what was typed: the Change is the text's.
        if (change.Property == SelectedItemProperty && IsEditable && !textFromSelection && !textFromTyping)
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
                // A choice is no Change: the component takes the item's text quietly. It selects nothing, the caret before the first
                // character; a user's pick then selects the whole text.
                instance.Set(P.Text, base.Text ?? "");
                selection.Select(0, 0);
            }
            finally
            {
                TextSearch.SetTextBinding(this, noItemMatches);
                textFromSelection = false;
            }
            return;
        }
        if (change.Property == TextProperty && IsEditable && !textFromSelection && !textFromComponent)
        {
            textFromTyping = true;
            try
            {
                base.OnPropertyChanged(change);
            }
            finally
            {
                textFromTyping = false;
            }
            instance.Set(P.Text, base.Text ?? "");
            if (style == ComboBoxStyle.DropdownCombo && editBox?.IsKeyboardFocusWithin == true)
                Narrow(base.Text ?? "");
            return;
        }
        base.OnPropertyChanged(change);
        if (change.Property == IsDropDownOpenProperty)
        {
            // A Simple Combo's list is always shown: it has nothing to drop, and raises no DropDown. A Locked combo's list does not drop.
            if (change.GetNewValue<bool>() && (style == ComboBoxStyle.SimpleCombo || locked))
                SetCurrentValue(IsDropDownOpenProperty, false);
            else if (change.GetNewValue<bool>())
                VBControl.Raise(this, VBEvents.DropDown);
            else
                EndNarrowing();
        }
        else if (change.Property == SelectedIndexProperty)
        {
            if (!searching)
                word = "";
            if (icons.Any)
                ShowSelectedIcon();
        }
    }

    /// <inheritdoc/>
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        icons.Show(container, item, index);
        ShowNarrowing(container, index);
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
        // A Dropdown List completes the word its characters add up; Avalonia's own search would match them as a prefix only.
        if (style != ComboBoxStyle.DropdownList)
        {
            base.OnTextInput(e);
            return;
        }
        if (e.Handled || string.IsNullOrEmpty(e.Text) || ItemCount == 0)
            return;
        e.Handled = true;
        if (locked)
            return;
        foreach (var character in e.Text)
            Search(character);
    }

    // The word grown by the character selects its best match. Where no item matches the grown word, the character starts a new one, which
    // selects the next item it matches after the selected one, as a ListBox's character does; where none matches the character either,
    // the word is forgotten and the selection stays.
    private void Search(char character)
    {
        var matches = word.Length > 0 ? ItemCompletion.Matches(Items, word + character) : [];
        int index;
        if (matches.Count > 0)
        {
            word += character;
            index = matches[0];
        }
        else
        {
            word = character.ToString();
            matches = ItemCompletion.Matches(Items, word);
            if (matches.Count == 0)
            {
                word = "";
                return;
            }
            index = matches[(matches.IndexOf(SelectedIndex) + 1) % matches.Count];
        }
        if (index == SelectedIndex)
            return;
        searching = true;
        try
        {
            SelectedIndex = index;
        }
        finally
        {
            searching = false;
        }
        if (IsDropDownOpen)
            ContainerFromIndex(index)?.Focus(NavigationMethod.Directional);
        Picked();
    }

    // A Dropdown Combo's typed text narrows its list to the items it matches, opening it. Where none matches, the list closes once the
    // box has taken the key, so that the closing does not select the text of the box.
    private void Narrow(string text)
    {
        var matches = ItemCompletion.Matches(Items, text);
        if (matches.Count == 0)
        {
            if (narrowed is not null)
                Dispatcher.UIThread.Post(CloseUnmatched);
            return;
        }
        narrowed = matches;
        highlighted = 0;
        if (!IsDropDownOpen)
            SetCurrentValue(IsDropDownOpenProperty, true);
        ShowNarrowing();
    }

    private void CloseUnmatched()
    {
        if (narrowed is null || !IsDropDownOpen || ItemCompletion.Matches(Items, base.Text ?? "").Count > 0)
            return;
        var (start, end) = editBox is { } box ? (box.SelectionStart, box.SelectionEnd) : (0, 0);
        SetCurrentValue(IsDropDownOpenProperty, false);
        if (editBox is { } closed)
        {
            closed.SelectionStart = start;
            closed.SelectionEnd = end;
        }
    }

    private void EndNarrowing()
    {
        if (narrowed is null)
            return;
        narrowed = null;
        ShowNarrowing();
    }

    private void ShowNarrowing()
    {
        foreach (var container in GetRealizedContainers())
            ShowNarrowing(container, IndexFromContainer(container));
        panel?.InvalidateArrange();
        if (narrowed is { } matches && ContainerFromIndex(matches[highlighted]) is { } best)
            best.BringIntoView();
    }

    // An item the text does not match is hidden; the highlighted match is drawn in the highlight's colours.
    private void ShowNarrowing(Control container, int index)
    {
        if (narrowed is null || narrowed.Contains(index))
            container.ClearValue(IsVisibleProperty);
        else
            container.IsVisible = false;
        if (container is not TemplatedControl item)
            return;
        if (narrowed is { } matches && matches[highlighted] == index)
        {
            item.Background = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.Highlight));
            item.Foreground = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.HighlightText));
        }
        else
        {
            item.ClearValue(BackgroundProperty);
            item.ClearValue(ForegroundProperty);
        }
    }

    // Where an item goes in the open list: a narrowed list's matches first, best first, then the items in their order.
    internal int RankOf(Control container)
    {
        var index = IndexFromContainer(container);
        if (narrowed is not { } matches)
            return index;
        var rank = matches.IndexOf(index);
        return rank >= 0 ? rank : matches.Count + index;
    }

    private void Choose(int index)
    {
        SetCurrentValue(IsDropDownOpenProperty, false);
        SelectedIndex = index;
        Picked();
    }

    // Up and Down move the selection at once, list open or closed, and raise Click when they move it; Enter in the open list only
    // closes it. Avalonia moves only the focus in an open list, and from no selection Up selects nothing, where VB's selects the
    // first item.
    private void TakeKey(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) || ItemCount == 0)
            return;
        // A narrowed list's keys move its highlight and choose, and leave the text as typed until then.
        if (narrowed is { } matches && IsDropDownOpen)
        {
            if (e.Key == Key.Enter)
                Choose(matches[highlighted]);
            else if (e.Key is Key.Up or Key.Down)
            {
                highlighted = Math.Clamp(highlighted + (e.Key == Key.Down ? 1 : -1), 0, matches.Count - 1);
                ShowNarrowing();
            }
            else
                return;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Enter && IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
            e.Handled = true;
            return;
        }
        if (e.Key is not (Key.Up or Key.Down))
            return;
        e.Handled = true;
        if (locked)
            return;
        var index = SelectedIndex < 0 ? 0 : Math.Clamp(SelectedIndex + (e.Key == Key.Down ? 1 : -1), 0, ItemCount - 1);
        if (index == SelectedIndex)
            return;
        SelectedIndex = index;
        if (IsDropDownOpen)
            ContainerFromIndex(index)?.Focus(NavigationMethod.Directional);
        Picked();
    }

    // A choice puts the item's text in the edit box selected, so that a key typed next replaces it. A Simple Combo's pick leaves the
    // focus in its edit box: what is typed next goes there.
    private void Picked()
    {
        if (style == ComboBoxStyle.SimpleCombo && editBox is { IsFocused: false } box)
            box.Focus(NavigationMethod.Pointer);
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
                // A text from code selects nothing, the caret before the first character.
                selection.Select(0, 0);
            }
            if (IsEditable && !textFromSelection && !replacing && !Equals(e.Before, e.After))
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
        else if (ReferenceEquals(e.Property, P.Locked))
        {
            locked = (bool)e.After!;
            if (editBox is { } box)
                box.IsReadOnly = locked;
            if (locked && IsDropDownOpen)
                SetCurrentValue(IsDropDownOpenProperty, false);
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

// A Dropdown Combo's open list: its items one under the other, in the combo's order of them, a narrowed list's matches first.
internal sealed class CompletionPanel(VBComboBox combo) : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var (width, height) = (0d, 0d);
        foreach (var child in Children)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var top = 0d;
        foreach (var child in Children.OrderBy(combo.RankOf))
        {
            child.Arrange(new Rect(0, top, finalSize.Width, child.DesiredSize.Height));
            top += child.DesiredSize.Height;
        }
        return finalSize;
    }
}

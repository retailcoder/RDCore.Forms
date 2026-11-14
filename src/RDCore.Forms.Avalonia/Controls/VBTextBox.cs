// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using ScrollBars = RDCore.Forms.Model.VB.ScrollBars;
using VBTextAlignment = RDCore.Forms.Model.VB.TextAlignment;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB TextBox: a <see cref="TextBox"/> that shows a <see cref="VBClasses.TextBox"/> component.
/// </summary>
/// <remarks>
/// <para>
/// The component's <c>Text</c> is the box's: typing sets it, and a change to it from code shows in the box. <c>Change</c> is raised
/// from the component's <c>Text</c>, whichever way it changed, and only when it changed, except that each step of a <c>SelText</c>
/// raises one, whether or not it changed the text.
/// </para>
/// <para>
/// The component's <c>SelStart</c> and <c>SelLength</c> are the box's selection: a selection set from code shows in the box, and the
/// user's sets them.
/// </para>
/// <para>
/// It raises KeyDown, KeyPress and KeyUp. A character typed is a KeyPress first, with its <c>KeyAscii</c>: a handler that sets it to
/// 0 cancels the character, and one that sets another code types that character instead. Backspace is KeyAscii 8, and Ctrl+A is 1.
/// Where its form's <c>KeyPreview</c> is True, the form raises each of them first: a KeyCode the form sets to 0 in KeyDown raises no
/// KeyDown on the box, and the box's KeyPress has the KeyAscii the form's left.
/// It raises Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove and MouseUp, MouseUp before Click.
/// </para>
/// </remarks>
public sealed class VBTextBox : TextBox, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;

    // Set while the box's text goes to the component, or the component's comes to the box.
    private bool syncing;

    // Set while the component's selection goes to the box, and the box's to the component.
    private bool selecting;

    static VBTextBox()
    {
        ControlEvents.AttachMouse<VBTextBox>(DblClick.InPlaceOfMouseDown);
        ControlEvents.AttachClick<VBTextBox>(ClickRelease.Anywhere);
        ControlEvents.AttachFocus<VBTextBox>();
        KeyDownEvent.AddClassHandler<VBTextBox>((box, e) => box.RaiseKey(e, VBEvents.KeyDown), RoutingStrategies.Tunnel);
        KeyUpEvent.AddClassHandler<VBTextBox>((box, e) => box.RaiseKey(e, VBEvents.KeyUp), RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Creates a box that shows a TextBox.
    /// </summary>
    /// <param name="instance">The TextBox.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a TextBox.</exception>
    public VBTextBox(ComponentInstance instance)
    {
        this.instance = instance;
        Padding = new Thickness(1);
        NewLine = "\r\n";
        ComponentBinding.Bind(this, instance, VBClasses.TextBox, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(TextBox);

    /// <summary>
    /// The component's <c>SelStart</c>: where the selection starts, or the caret's place where nothing is selected; 0 is before the
    /// first character.
    /// </summary>
    /// <exception cref="FormErrorException">Set below 0 or above 65535: 380, Invalid property value.</exception>
    /// <remarks>
    /// A value past the end of the text puts the caret at the end. Setting it selects nothing.
    /// </remarks>
    public int SelStart
    {
        get => instance.Get(P.SelStart);
        set => Select(P.SelStart, value);
    }

    /// <summary>
    /// The component's <c>SelLength</c>: the number of characters selected, from <see cref="SelStart"/>.
    /// </summary>
    /// <exception cref="FormErrorException">Set below 0 or above 65535: 380, Invalid property value.</exception>
    /// <remarks>
    /// A value past the end of the text selects up to the end.
    /// </remarks>
    public int SelLength
    {
        get => instance.Get(P.SelLength);
        set => Select(P.SelLength, value);
    }

    /// <summary>
    /// The component's <c>SelText</c>: the selected text; "" where nothing is selected.
    /// </summary>
    /// <exception cref="ArgumentNullException">Set to <see langword="null"/>.</exception>
    /// <remarks>
    /// Setting it puts the text in place of the selection, or at the caret, and the caret after it, with nothing selected. Where the
    /// box has a <c>MaxLength</c>, what does not fit is left out. A <c>Locked</c> box takes it too. A single-line box raises Change
    /// once, even where its text does not change, and none where nothing is selected and a text other than "" does not fit at all. A
    /// multi-line box takes it in two steps, each with its Change: the selection is deleted, where there is one, then the text is
    /// inserted, where some of it fits.
    /// </remarks>
    public string SelText
    {
        get => instance.Get(P.SelText);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Select(P.SelText, value);
        }
    }

    private void Select(PropertyDefinition property, object value)
    {
        if (!instance.TrySet(property, value))
            throw new FormErrorException(FormError.InvalidPropertyValue);
    }

    // The component's selection, in the box. The caret goes to the selection's end first: setting it empties the selection. The
    // component's selection can start or end between the CR and the LF of a new line, where the box's caret does not stop: the box
    // shows it as near as it can.
    private void ShowSelection()
    {
        var length = (Text ?? "").Length;
        var start = Math.Min(instance.Get(P.SelStart), length);
        var end = Math.Min(start + instance.Get(P.SelLength), length);
        selecting = true;
        try
        {
            CaretIndex = end;
            SelectionStart = start;
            SelectionEnd = end;
        }
        finally
        {
            selecting = false;
        }
    }

    // The user's selection, from its anchor to its caret, goes to the component.
    private void TakeSelection()
    {
        selecting = true;
        try
        {
            instance.Set(P.SelStart, Math.Min(SelectionStart, SelectionEnd));
            instance.Set(P.SelLength, Math.Abs(SelectionEnd - SelectionStart));
        }
        finally
        {
            selecting = false;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // A property set as the box is made comes before its component is.
        if (instance is null)
            return;
        if (change.Property == TextProperty && !syncing)
        {
            // The text the user changes selects nothing in the component: the box's caret, which a deletion leaves in place, is put back.
            Sync(() => instance.Set(P.Text, Text ?? ""));
            TakeSelection();
        }
        else if ((change.Property == SelectionStartProperty || change.Property == SelectionEndProperty || change.Property == CaretIndexProperty)
            && !selecting && !syncing)
            TakeSelection();
    }

    /// <inheritdoc/>
    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (e.Handled || string.IsNullOrEmpty(e.Text))
            return;
        // A character outside ASCII is typed with no KeyPress: its KeyAscii, a code of the system's ANSI code page, is not read.
        if (e.Text.Any(character => character > 127))
        {
            base.OnTextInput(e);
            return;
        }
        e.Handled = true;
        foreach (var character in e.Text)
        {
            var ascii = KeyPress(character);
            if (ascii != 0)
                base.OnTextInput(new TextInputEventArgs { Text = ((char)ascii).ToString() });
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && (e.Key == Key.Back || e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control))
        {
            var ascii = KeyPress(e.Key == Key.Back ? 8 : 1);
            if (ascii == 0)
            {
                e.Handled = true;
                return;
            }
            if (ascii is not (8 or 1))
            {
                e.Handled = true;
                base.OnTextInput(new TextInputEventArgs { Text = ((char)ascii).ToString() });
                return;
            }
        }
        // Enter and Esc, where no button took them, type nothing in a single-line box. In a multi-line one, Enter and Ctrl+Enter type
        // a new line, CR LF, unless the KeyPress cancels it or types another character.
        else if (!e.Handled && ControlKeys.AsciiOf(e) is { } code)
        {
            var ascii = KeyPress(code);
            if (code is 13 or 10 && instance.Get(P.MultiLine))
            {
                e.Handled = true;
                if (ascii != 0)
                    base.OnTextInput(new TextInputEventArgs { Text = ascii is 13 or 10 ? "\r\n" : ((char)ascii).ToString() });
                return;
            }
        }
        base.OnKeyDown(e);
    }

    // The KeyAscii the handlers left, the form's first, the code typed where there are none. One outside a character's range types
    // nothing.
    private int KeyPress(int ascii) => ControlKeys.KeyPress(this, (short)ascii) ?? 0;

    private void RaiseKey(KeyEventArgs e, EventDefinition definition)
    {
        if (!e.Handled && VBControl.Owns(this, e.Source) && KeyCodes.Of(e) is { } code)
            ControlKeys.Raise(this, definition, code, ControlEvents.ShiftOf(e.KeyModifiers));
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.Text))
        {
            var text = (string)e.After!;
            if (!syncing && Text != text)
                Sync(() => Text = text);
            if (!Equals(e.Before, e.After) || ReferenceEquals(e.AssignedProperty, P.SelText))
                VBControl.Raise(this, VBEvents.Change);
        }
        else if (ReferenceEquals(e.Property, P.SelStart) || ReferenceEquals(e.Property, P.SelLength))
        {
            if (!syncing && !selecting)
                ShowSelection();
        }
        else if (ReferenceEquals(e.Property, P.SelText))
        {
            // Made from the text and the selection, which show it.
        }
        else if (ReferenceEquals(e.Property, P.Locked))
            IsReadOnly = (bool)e.After!;
        else if (ReferenceEquals(e.Property, P.Alignment))
            TextAlignment = ToAvalonia((VBTextAlignment)e.After!);
        else if (ReferenceEquals(e.Property, P.BorderStyle))
            BorderThickness = (BorderStyle)e.After! == BorderStyle.None ? new Thickness(0) : new Thickness(1);
        else if (ReferenceEquals(e.Property, P.MultiLine) || ReferenceEquals(e.Property, P.ScrollBars))
            ApplyLines();
        else if (ReferenceEquals(e.Property, P.PasswordChar))
            PasswordChar = e.After is string { Length: 1 } character ? character[0] : '\0';
        else if (ReferenceEquals(e.Property, P.MaxLength))
            MaxLength = (int)e.After!;
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }

    // A single-line box scrolls along its line with no scroll bar. A multi-line box wraps its lines unless it has a horizontal scroll
    // bar, and scrolls down them with or without a vertical one.
    private void ApplyLines()
    {
        var multiLine = instance.Get(P.MultiLine);
        var bars = multiLine ? instance.Get(P.ScrollBars) : ScrollBars.None;
        var horizontal = bars is ScrollBars.Horizontal or ScrollBars.Both;
        AcceptsReturn = multiLine;
        TextWrapping = multiLine && !horizontal ? TextWrapping.Wrap : TextWrapping.NoWrap;
        ScrollViewer.SetHorizontalScrollBarVisibility(this,
            horizontal ? ScrollBarVisibility.Visible : multiLine ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden);
        ScrollViewer.SetVerticalScrollBarVisibility(this,
            bars is ScrollBars.Vertical or ScrollBars.Both ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden);
    }

    private void Sync(Action action)
    {
        syncing = true;
        try
        {
            action();
        }
        finally
        {
            syncing = false;
        }
    }

    internal static global::Avalonia.Media.TextAlignment ToAvalonia(VBTextAlignment alignment) => alignment switch
    {
        VBTextAlignment.RightJustify => global::Avalonia.Media.TextAlignment.Right,
        VBTextAlignment.Center => global::Avalonia.Media.TextAlignment.Center,
        _ => global::Avalonia.Media.TextAlignment.Left,
    };
}

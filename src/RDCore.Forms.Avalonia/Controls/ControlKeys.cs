using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Avalonia.Controls;

// A control's own KeyDown, KeyPress and KeyUp, as measured with VB6, native and p-code agreeing: KeyDown, then KeyPress, then what the
// key does (a list's Click, a combo's Change), then KeyUp. Where the form's KeyPreview is True, the form raises each one first
// (FormKeys). A key with no KeyCode raises none (KeyCodes), and a character outside ASCII raises no KeyPress.
//  - KeyPress has the TextBox's rules: a KeyAscii of 0 types nothing, another code is taken in place of the character, and one outside
//    a character's range types nothing. Backspace is KeyAscii 8 and Ctrl+A is 1, and only 0 changes what they do.
//  - Space's KeyPress is raised with its KeyDown: a button takes Space as it goes down, and the window system then types no space.
//    Its KeyAscii is the space typed after it, where one is.
//  - On a button, a check box and an option button, an arrow moves the focus and raises no key event, the form's included. A button,
//    check box or option button that takes the focus raises no KeyUp for it either; another control raises its KeyUp alone.
// The handlers go on the way down, so that they come before the control's own handling of the key.
internal static class ControlKeys
{
    // The control whose Space raised its KeyPress with its KeyDown, and the code its handlers left, for the space it may still type.
    private static (Control Control, short Code)? spacePressed;

    public static void Attach<T>(bool arrowsMoveTheFocus = false) where T : Control, IRaisesKeyEvents
    {
        InputElement.KeyDownEvent.AddClassHandler<T>((control, e) => KeyDown(control, e, arrowsMoveTheFocus), RoutingStrategies.Tunnel);
        InputElement.KeyUpEvent.AddClassHandler<T>((control, e) =>
        {
            if (e.Key == Key.Space)
                spacePressed = null;
            if (Takes(control, e, arrowsMoveTheFocus) is { } code)
                Raise(control, VBEvents.KeyUp, code, ControlEvents.ShiftOf(e.KeyModifiers));
        }, RoutingStrategies.Tunnel);
        InputElement.TextInputEvent.AddClassHandler<T>(TextInput, RoutingStrategies.Tunnel);
    }

    // Raises a key event of the control, its form's first, and answers the code its handlers left: 0 where the form's KeyDown or
    // KeyPress set it to 0, and so raised none on the control.
    public static short Raise(Control control, EventDefinition definition, short code, short? shift = null)
    {
        var own = FormKeys.Preview(control, definition, code, shift);
        if (own == 0)
            return 0;
        FormValue[] arguments = shift is { } mask
            ? [FormValue.FromInteger(own), FormValue.FromInteger(mask)]
            : [FormValue.FromInteger(own)];
        return VBControl.Raise(control, definition, arguments) is { } after && after[0].Kind == FormValueKind.Integer
            ? after[0].AsInteger()
            : own;
    }

    // The character a KeyPress leaves to type, the form's first: none for 0 and for a code outside a character's range.
    public static char? KeyPress(Control control, short ascii) =>
        Raise(control, VBEvents.KeyPress, ascii) is var code and > 0 and <= 255 ? (char)code : null;

    // The innermost control of a component that an element is in, or none. An open list is in its own popup, whose visual tree is
    // not the control's: its way back is through its logical parents.
    public static Control? Owner(object? element)
    {
        for (var step = element as StyledElement; step is not null; step = Up(step))
            if (step is Control control && VBControl.GetInstance(control) is not null)
                return control;
        return null;
    }

    // A popup's host, in its own window or in the window's overlay, goes on from its popup, in the control that opened it. A window
    // ends the walk: its host's logical parent is the window again.
    private static StyledElement? Up(StyledElement step) => step switch
    {
        PopupRoot or OverlayPopupHost when step.Parent is { } popup => popup,
        TopLevel => null,
        Visual visual => visual.GetVisualParent() ?? step.Parent,
        _ => step.Parent,
    };

    private static void KeyDown(Control control, KeyEventArgs e, bool arrowsMoveTheFocus)
    {
        if (Takes(control, e, arrowsMoveTheFocus) is not { } code)
            return;
        Raise(control, VBEvents.KeyDown, code, ControlEvents.ShiftOf(e.KeyModifiers));
        // Its 0 types no space, and leaves the key to the control.
        if (e.Key == Key.Space && e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift)
            spacePressed = (control, KeyPress(control, 32) is { } character ? (short)character : (short)0);
        // Backspace and Ctrl+A type a character with no text.
        else if ((e.Key == Key.Back || e.Key == Key.A && e.KeyModifiers == KeyModifiers.Control)
                 && KeyPress(control, e.Key == Key.Back ? (short)8 : (short)1) is null)
            e.Handled = true;
    }

    private static void TextInput(Control control, TextInputEventArgs e)
    {
        if (e.Handled || string.IsNullOrEmpty(e.Text) || e.Text.Any(character => character > 127) || !ReferenceEquals(Owner(e.Source), control))
            return;
        var typed = new System.Text.StringBuilder(e.Text.Length);
        foreach (var character in e.Text)
        {
            // The space of a Space whose KeyPress its KeyDown raised.
            if (character == ' ' && spacePressed is { } space && ReferenceEquals(space.Control, control))
            {
                spacePressed = null;
                if (space.Code != 0)
                    typed.Append((char)space.Code);
            }
            else if (KeyPress(control, (short)character) is { } kept)
                typed.Append(kept);
        }
        if (typed.Length == 0)
            e.Handled = true;
        else
            e.Text = typed.ToString();
    }

    // The KeyCode of a key the control raises its events for, or none.
    private static short? Takes(Control control, KeyEventArgs e, bool arrowsMoveTheFocus)
    {
        if (e.Handled || !ReferenceEquals(Owner(e.Source), control)
            || arrowsMoveTheFocus && e.KeyModifiers == KeyModifiers.None && e.Key is Key.Left or Key.Up or Key.Right or Key.Down)
            return null;
        return KeyCodes.Of(e);
    }
}

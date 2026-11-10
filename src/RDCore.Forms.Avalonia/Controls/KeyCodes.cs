// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia.Input;

namespace RDCore.Forms.Avalonia.Controls;

// The KeyCode VB gives a key in KeyDown and KeyUp, as measured with VB6, native and p-code agreeing. A key VB6 was not measured with,
// and a chord of several modifiers, has none, and raises nothing. Enter and Esc have theirs where no button takes them
// (VBFormWindow); Esc with Ctrl or Alt has none. Tab is not among them: it waits for the form's tab order.
internal static class KeyCodes
{
    public static short? Of(KeyEventArgs e)
    {
        var modifiers = e.KeyModifiers;
        if (modifiers is not (KeyModifiers.None or KeyModifiers.Shift or KeyModifiers.Control or KeyModifiers.Alt)
            && e.Key is not (Key.K or Key.A or Key.X or Key.Back or Key.Delete or Key.Left or Key.F2
                or Key.LeftShift or Key.RightShift or Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt))
            return null;
        var code = e.Key switch
        {
            >= Key.A and <= Key.Z => 65 + (e.Key - Key.A),
            >= Key.D0 and <= Key.D9 => 48 + (e.Key - Key.D0),
            >= Key.NumPad0 and <= Key.NumPad9 when modifiers == KeyModifiers.None => 96 + (e.Key - Key.NumPad0),
            >= Key.F1 and <= Key.F12 when e.Key != Key.F10 && !(e.Key == Key.F4 && modifiers.HasFlag(KeyModifiers.Alt)) => 112 + (e.Key - Key.F1),
            Key.Space when !modifiers.HasFlag(KeyModifiers.Alt) => 32,
            Key.Back => 8,
            Key.Enter => 13,
            Key.Escape when (modifiers & ~KeyModifiers.Shift) == KeyModifiers.None => 27,
            Key.Insert => 45,
            Key.Delete => 46,
            Key.Home => 36,
            Key.End => 35,
            Key.PageUp => 33,
            Key.PageDown => 34,
            Key.Left => 37,
            Key.Up => 38,
            Key.Right => 39,
            Key.Down => 40,
            Key.Clear => 12,
            Key.Decimal when modifiers == KeyModifiers.None => 110,
            Key.Add when (modifiers & ~KeyModifiers.Shift) == KeyModifiers.None => 107,
            Key.Subtract when (modifiers & ~KeyModifiers.Shift) == KeyModifiers.None => 109,
            Key.Multiply when (modifiers & ~KeyModifiers.Shift) == KeyModifiers.None => 106,
            Key.Divide when (modifiers & ~KeyModifiers.Shift) == KeyModifiers.None => 111,
            Key.NumLock when modifiers == KeyModifiers.None => 144,
            Key.LeftShift or Key.RightShift => 16,
            Key.LeftCtrl or Key.RightCtrl => 17,
            Key.LeftAlt or Key.RightAlt => 18,
            _ => 0,
        };
        return code == 0 ? null : (short)code;
    }
}

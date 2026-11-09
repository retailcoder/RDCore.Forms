// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia.Controls;
using Avalonia.Input;

namespace RDCore.Forms.Avalonia.Controls;

// The arrow keys on an OptionButton, a CheckBox or a CommandButton move the focus to the previous or next control in its own
// container, as measured with VB6:
//  - Up and Left go to the previous control, Down and Right to the next, by TabIndex, wrapping within the container.
//  - The control reached need not be a button, and its TabStop does not matter. A control that cannot take the focus is passed
//    over: a disabled one, a Label, and a Frame, without going into it.
//  - An OptionButton it lands on is selected, as the focus selects one. A CheckBox is not changed.
// A TextBox, a PictureBox and the lists keep the arrow keys.
internal static class ArrowNavigation
{
    public static void Attach<T>() where T : Control =>
        InputElement.KeyDownEvent.AddClassHandler<T>((control, e) =>
        {
            // With a modifier held, VB6 was not measured, so nothing is done.
            if (e.Handled || !ReferenceEquals(e.Source, control) || e.KeyModifiers != KeyModifiers.None)
                return;
            var step = e.Key switch
            {
                Key.Up or Key.Left => -1,
                Key.Down or Key.Right => 1,
                _ => 0,
            };
            if (step == 0 || Next(control, step) is not { } next)
                return;
            e.Handled = true;
            next.Focus(NavigationMethod.Directional);
        });

    // The previous or next control in the container that can take the focus, by TabIndex, wrapping, or null where there is no other.
    // The sort is stable, so two controls with one TabIndex keep their order.
    private static InputElement? Next(Control from, int step)
    {
        if (from.Parent is not Panel container)
            return null;
        var order = container.Children.OfType<InputElement>()
            .Where(c => ReferenceEquals(c, from) || c is { Focusable: true, IsEffectivelyEnabled: true, IsEffectivelyVisible: true })
            .OrderBy(KeyboardNavigation.GetTabIndex)
            .ToList();
        var at = order.IndexOf(from);
        return at < 0 || order.Count < 2 ? null : order[(at + step + order.Count) % order.Count];
    }
}

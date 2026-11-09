// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using MousePointer = RDCore.Forms.Model.VB.MousePointer;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

// Keeps a control showing its component. Every property of the component's class is applied when the control is made and again
// when it joins a logical tree, and each change is applied as the component raises it, while the control is in one: a control
// taken out of its form holds no handler of its component's.
internal static class ComponentBinding
{
    public static void Bind(Control control, ComponentInstance instance, ComponentClass expected, Action<ComponentPropertyChangedEventArgs> changed)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!ReferenceEquals(instance.Class, expected))
            throw new ArgumentException($"The component is a {instance.Class.TypeName}, not a {expected.TypeName}.", nameof(instance));

        VBControl.SetInstance(control, instance);
        void OnChanged(object? sender, ComponentPropertyChangedEventArgs e) => changed(e);
        void ApplyAll()
        {
            foreach (var property in instance.Class.Properties)
            {
                var value = instance.GetBoxed(property);
                changed(new ComponentPropertyChangedEventArgs(property, value, value));
            }
        }
        ApplyAll();
        control.AttachedToLogicalTree += (_, _) =>
        {
            instance.PropertyChanged -= OnChanged;
            instance.PropertyChanged += OnChanged;
            ApplyAll();
        };
        control.DetachedFromLogicalTree += (_, _) => instance.PropertyChanged -= OnChanged;
    }

    // Applies a property every control class shares, and answers whether it was one.
    public static bool ApplyCommon(Control control, PropertyDefinition property, object? value)
    {
        switch (property.Name)
        {
            case nameof(P.Enabled):
                control.IsEnabled = (bool)value!;
                return true;
            case nameof(P.ToolTipText):
                ToolTip.SetTip(control, value is string { Length: > 0 } tip ? tip : null);
                return true;
            case nameof(P.MousePointer):
                control.Cursor = ToCursor((MousePointer)value!);
                return true;
            case nameof(P.TabIndex):
                KeyboardNavigation.SetTabIndex(control, (short)value!);
                return true;
            case nameof(P.TabStop):
                KeyboardNavigation.SetIsTabStop(control, (bool)value!);
                return true;
            case nameof(P.Font):
                FormFonts.Apply(control, (FormFont)value!);
                return true;
        }
        if (control is not TemplatedControl templated)
            return false;
        switch (property.Name)
        {
            case nameof(P.BackColor):
                templated.Background = FormPalette.ToBrush((FormColor)value!);
                return true;
            case nameof(P.ForeColor):
                templated.Foreground = FormPalette.ToBrush((FormColor)value!);
                return true;
        }
        return false;
    }

    // The pointer VB draws for a MousePointer. Default is the control's own, such as a TextBox's I-beam. Icon is the plain arrow,
    // and Size the four-way size pointer, as VB6 draws them on Windows 10. A Custom pointer, drawn from MouseIcon, is the arrow.
    public static Cursor? ToCursor(MousePointer pointer) => pointer switch
    {
        MousePointer.Default => null,
        MousePointer.Cross => new Cursor(StandardCursorType.Cross),
        MousePointer.IBeam => new Cursor(StandardCursorType.Ibeam),
        MousePointer.Size or MousePointer.SizeAll => new Cursor(StandardCursorType.SizeAll),
        MousePointer.SizeNESW => new Cursor(StandardCursorType.BottomLeftCorner),
        MousePointer.SizeNS => new Cursor(StandardCursorType.SizeNorthSouth),
        MousePointer.SizeNWSE => new Cursor(StandardCursorType.BottomRightCorner),
        MousePointer.SizeWE => new Cursor(StandardCursorType.SizeWestEast),
        MousePointer.UpArrow => new Cursor(StandardCursorType.UpArrow),
        MousePointer.Hourglass => new Cursor(StandardCursorType.Wait),
        MousePointer.NoDrop => new Cursor(StandardCursorType.No),
        MousePointer.ArrowHourglass => new Cursor(StandardCursorType.AppStarting),
        MousePointer.ArrowQuestion => new Cursor(StandardCursorType.Help),
        _ => new Cursor(StandardCursorType.Arrow),
    };

    // The brush of a form colour, for a control that paints it itself.
    public static IBrush Brush(object? value) => FormPalette.ToBrush((FormColor)value!);
}

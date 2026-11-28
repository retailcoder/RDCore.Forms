// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using RDCore.Forms.Model;
using MousePointer = RDCore.Forms.Model.MousePointer;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

// Keeps a control showing its component. Every property of the component's class is applied when the control is made and again
// when it joins a logical tree, and each change is applied as the component raises it, while the control is in one: a control
// taken out of its form holds no handler of its component's.
internal static class ComponentBinding
{
    // The component whose properties are being applied again, which no program set: VB fits a Label to its caption as AutoSize is set,
    // even to the value it has, and never as the form loads.
    [ThreadStatic]
    private static ComponentInstance? reapplied;

    // Whether the change being applied is a property applied again, as the control is made, joins a logical tree or takes a new theme.
    public static bool IsReapplying(ComponentInstance instance) => ReferenceEquals(reapplied, instance);

    public static void Bind(Control control, ComponentInstance instance, ComponentClass expected, Action<ComponentPropertyChangedEventArgs> changed)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!ReferenceEquals(instance.Class, expected))
            throw new ArgumentException($"The component is a {instance.Class.TypeName}, not a {expected.TypeName}.", nameof(instance));

        RDFormsControl.SetInstance(control, instance);
        void OnChanged(object? sender, ComponentPropertyChangedEventArgs e) => changed(e);
        void ApplyAll()
        {
            var outer = reapplied;
            reapplied = instance;
            try
            {
                foreach (var property in instance.Class.Properties)
                {
                    var value = instance.GetBoxed(property);
                    changed(new ComponentPropertyChangedEventArgs(property, value, value));
                }
            }
            finally
            {
                reapplied = outer;
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
        // A system colour is drawn in the control's theme variant: a new variant, from its window's KeepSystemTheme or from the
        // application's theme while it is kept, applies every property again and redraws what the control paints itself.
        control.ActualThemeVariantChanged += (_, _) =>
        {
            ApplyAll();
            control.InvalidateVisual();
        };
    }

    // Applies a property every control class shares, and answers whether it was one.
    //  - Left, Top, Width and Height are pixels whatever the container's ScaleMode, and place the control in its container's canvas
    //    as they are.
    //  - Visible is the control's own. A control in a hidden container keeps its Visible, is not drawn and takes no focus, and its
    //    events still come from its component.
    //  - Enabled is the control's own too. A control in a disabled container keeps its Enabled and takes no input.
    //  - Opacity is a mask over the control and what it holds: it leaves the control's own Opacity to the theme, which fades a disabled
    //    control, and a control the mask hides still takes the mouse.
    public static bool ApplyCommon(Control control, PropertyDefinition property, object? value)
    {
        switch (property.Name)
        {
            case nameof(P.Left):
                Canvas.SetLeft(control, (double)value!);
                return true;
            case nameof(P.Top):
                Canvas.SetTop(control, (double)value!);
                return true;
            case nameof(P.Width):
                control.Width = (double)value!;
                return true;
            case nameof(P.Height):
                control.Height = (double)value!;
                return true;
            case nameof(P.Visible):
                control.IsVisible = (bool)value!;
                return true;
            case nameof(P.Enabled):
                control.IsEnabled = (bool)value!;
                return true;
            case nameof(P.Opacity):
                control.OpacityMask = (double)value! is var opacity && opacity < 1 ? new SolidColorBrush(Colors.Black, opacity) : null;
                return true;
            case nameof(P.ToolTipText):
                ToolTip.SetTip(control, value is string { Length: > 0 } tip ? tip : null);
                return true;
            case nameof(P.MousePointer) or nameof(P.MouseIcon):
                ShowPointer(control);
                // The controls it holds whose MousePointer is Default show its pointer, or the next container's. Applied again, as they
                // join the tree or take a new theme, they apply their own.
                if (IsReapplying(RDFormsControl.GetInstance(control)!))
                    return true;
                foreach (var held in control.GetVisualDescendants().OfType<Control>())
                    if (RDFormsControl.GetInstance(held) is { } component && component.Class.Has(P.MousePointer)
                        && component.Get(P.MousePointer) == MousePointer.Default)
                        ShowPointer(held);
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
                templated.Background = Brush(control, value);
                return true;
            case nameof(P.ForeColor):
                templated.Foreground = Brush(control, value);
                return true;
        }
        return false;
    }

    // A control shows its component's pointer. Where its MousePointer is Default, it shows the pointer of its nearest container (a
    // Frame, a PictureBox, then the form) whose MousePointer is not, as VB6 does, and else its own, such as a TextBox's I-beam: the
    // pointer its theme gives it.
    private static void ShowPointer(Control control)
    {
        var instance = RDFormsControl.GetInstance(control)!;
        var from = instance;
        while (from is not null && Pointer(from) == MousePointer.Default)
            from = from.Container;
        var cursor = from is null ? null : PointerOf(from);
        Show(control, cursor);
        // A theme sets the pointer of parts of its template, such as a TextBox's text area, over the control's own, and may style a part
        // after it is made: the pointer shown is set on every part, and taken off them gives them the theme's again. The parts are there
        // once the control's template, and the template of each part that has one, is applied, and once the content of each presenter
        // among them joins the tree: the pointer is shown again then.
        Follow(control, control);
        foreach (var part in Parts(control))
        {
            if (part is InputElement element)
                Show(element, cursor);
            Follow(part, control);
        }

        static void Show(InputElement element, Cursor? cursor)
        {
            if (cursor is null)
                element.ClearValue(InputElement.CursorProperty);
            else
                element.Cursor = cursor;
        }

        static void Follow(Visual part, Control control)
        {
            if (part is TemplatedControl templated && Followed.TryAdd(templated, null))
                templated.TemplateApplied += (_, _) => ShowPointer(control);
            else if (part is ContentPresenter presenter && Followed.TryAdd(presenter, null))
                // A presenter takes its content before the content joins the visual tree: the pointer is shown as it joins.
                presenter.PropertyChanged += (_, e) =>
                {
                    if (e.Property == ContentPresenter.ChildProperty && e.NewValue is Visual content && Followed.TryAdd(content, null))
                        content.AttachedToVisualTree += (_, _) => ShowPointer(control);
                };
        }
    }

    // The parts whose template or content the pointer follows.
    private static readonly ConditionalWeakTable<Visual, object?> Followed = new();

    // The parts of a control's template: none inside a control it holds, and none of its scroll bars, which keep their own pointer, as
    // the scroll bars of VB6's windows are outside the area VB6 sets the pointer of.
    private static IEnumerable<Visual> Parts(Visual control)
    {
        foreach (var child in control.GetVisualChildren())
        {
            if (child is Control held && RDFormsControl.GetInstance(held) is not null || child is ScrollBar)
                continue;
            yield return child;
            foreach (var part in Parts(child))
                yield return part;
        }
    }

    private static MousePointer Pointer(ComponentInstance instance) =>
        instance.Class.Has(P.MousePointer) ? instance.Get(P.MousePointer) : MousePointer.Default;

    // The pointer a component's MousePointer names: a Custom pointer is made from its MouseIcon, and is the arrow where it has none.
    private static Cursor? PointerOf(ComponentInstance instance)
    {
        var pointer = Pointer(instance);
        if (pointer is MousePointer.Custom && instance.Class.Has(P.MouseIcon))
        {
            using var image = RDFormsPicture.DecodePointer(instance.Get(P.MouseIcon), out var hotSpot);
            if (image is not null)
                return new Cursor(image, hotSpot);
        }
        return ToCursor(pointer);
    }

    // The pointer VB draws for a MousePointer. Default is the control's own, such as a TextBox's I-beam. Icon is the plain arrow,
    // and Size the four-way size pointer, as VB6 draws them on Windows 10. Custom, with no MouseIcon, is the arrow.
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

    // Runs an action now and again each time the control's theme variant changes: for a system colour the control takes of its own,
    // that no property of its component names.
    public static void FollowVariant(Control control, Action apply)
    {
        apply();
        control.ActualThemeVariantChanged += (_, _) => apply();
    }

    // The brush of a form colour, in the control's theme variant, for a control that paints it itself.
    public static IBrush Brush(Control control, object? value) => FormPalette.ToBrush((FormColor)value!, control.ActualThemeVariant);

    // The brush of a system colour, in the control's theme variant.
    public static IBrush Brush(Control control, SystemColor color) => FormPalette.ToBrush(FormColor.FromSystem(color), control.ActualThemeVariant);

    // The colour of a form colour, in the control's theme variant.
    public static Color ColorOf(Control control, FormColor color) => FormPalette.ToColor(color, control.ActualThemeVariant);
}

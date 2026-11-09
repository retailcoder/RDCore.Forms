// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Rendering;

/// <summary>
/// A form's client area: a canvas that shows the controls of a <see cref="FormDefinition"/>, placed, stacked and ordered as VB
/// places, stacks and orders them.
/// </summary>
/// <remarks>
/// <para>
/// Every component of the form that <see cref="VBControls.Create"/> makes a control for is shown, in its container's canvas. A
/// menu item, which it makes no control for, is not shown, and neither is anything placed in one. A Timer's control is on the form
/// and draws nothing.
/// </para>
/// <para>
/// A control's <c>Left</c>, <c>Top</c>, <c>Width</c> and <c>Height</c> are twips whatever its container's <c>ScaleMode</c>, as a
/// designer file holds them: a control is placed at 15 twips a pixel. In each container the first control the form holds is on
/// top, and a windowless control (a Label or an Image) is under every windowed one, whatever their order.
/// </para>
/// <para>
/// Tab and Shift+Tab move the focus through one order across the whole form, a control in a Frame or a PictureBox included: by
/// <c>TabIndex</c>, the order the form holds them in deciding a tie. A <c>TabIndex</c> past the last place takes the first place no
/// other control has, in the order the form holds them. The focus skips a control that cannot take it: one whose <c>TabStop</c> is
/// False, a Label or a Frame, and one that is hidden or disabled, or in a hidden or disabled container.
/// </para>
/// <para>
/// The view follows the root's <c>Width</c>, <c>Height</c>, <c>BackColor</c>, <c>Enabled</c>, <c>Font</c> and
/// <c>MousePointer</c>. A disabled root takes no input in any of its controls.
/// </para>
/// </remarks>
public sealed class VBFormView : Canvas
{
    // The windowless plane's top: a container would need a million windowed controls to reach it.
    private const int WindowlessTop = -1_000_000;

    private readonly Dictionary<ComponentInstance, Control> controls = new(ReferenceEqualityComparer.Instance);
    // The shown components, in the order the form holds them.
    private readonly List<ComponentInstance> shown = [];

    /// <summary>
    /// Creates the client area of a form.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <see langword="null"/>.</exception>
    public VBFormView(FormDefinition form)
    {
        ArgumentNullException.ThrowIfNull(form);
        Form = form;
        Place(form.Root, this);
        ComponentBinding.Bind(this, form.Root, form.Root.Class, Apply);
        AddHandler(KeyDownEvent, OnKeyDown);
    }

    /// <summary>
    /// The form the view shows.
    /// </summary>
    public FormDefinition Form { get; }

    /// <summary>
    /// The controls that take part in the form's tab order, in it: every shown control whose class has a <c>TabIndex</c>, whether
    /// or not it can take the focus now.
    /// </summary>
    public IReadOnlyList<Control> TabOrder => [.. TabOrderOf().Select(c => controls[c])];

    /// <summary>
    /// Gets the control that shows a component of the form.
    /// </summary>
    /// <param name="component">The component.</param>
    /// <returns>The control, or <see langword="null"/> where the component is not shown.</returns>
    public Control? ControlOf(ComponentInstance component) => controls.GetValueOrDefault(component);

    private void Place(ComponentInstance container, Canvas host)
    {
        int windowed = 0, windowless = WindowlessTop;
        foreach (var component in container.Contents)
        {
            if (VBControls.Create(component) is not { } control)
                continue;
            // Each control goes under the ones of its plane placed before it.
            control.ZIndex = component.Class.IsWindowless ? windowless-- : windowed--;
            host.Children.Add(control);
            controls.Add(component, control);
            shown.Add(component);
            if (control is IVBContainer inner)
                Place(component, inner.ChildHost);
        }
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            // The root's own place is on the screen, and its Visible whether its window is shown.
            case nameof(P.Left) or nameof(P.Top) or nameof(P.Visible):
                break;
            case nameof(P.BackColor):
                Background = ComponentBinding.Brush(e.After);
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }

    // The shown components with a TabIndex, in the form's tab order.
    private List<ComponentInstance> TabOrderOf()
    {
        var indexed = shown.Where(c => c.Class.Has(P.TabIndex)).ToList();
        var places = new Dictionary<ComponentInstance, int>(ReferenceEqualityComparer.Instance);
        foreach (var component in indexed)
            places[component] = component.Get(P.TabIndex);
        var taken = places.Values.Where(place => place < indexed.Count).ToHashSet();
        foreach (var component in indexed.Where(c => places[c] >= indexed.Count))
        {
            var free = Enumerable.Range(0, indexed.Count).First(place => !taken.Contains(place));
            places[component] = free;
            taken.Add(free);
        }
        // OrderBy is stable, so a tie keeps the form's order.
        return [.. indexed.OrderBy(c => places[c])];
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Tab || e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Shift))
            return;
        var order = TabOrderOf().Select(c => controls[c]).ToList();
        var forward = e.KeyModifiers == KeyModifiers.None;
        var from = Focused(order);
        // From the control after the focused one, round to the focused one itself; from the first, where none has the focus.
        var start = from is null ? (forward ? 0 : order.Count - 1) : order.IndexOf(from) + (forward ? 1 : -1);
        for (var step = 0; step < order.Count; step++)
        {
            var candidate = order[((start + (forward ? step : -step)) % order.Count + order.Count) % order.Count];
            if (!CanTakeTheFocus(candidate))
                continue;
            e.Handled = true;
            candidate.Focus(NavigationMethod.Tab, e.KeyModifiers);
            return;
        }
    }

    // The control of the order that has the focus, or one of whose parts has it, such as a ComboBox's text box.
    private Control? Focused(List<Control> order)
    {
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        for (var visual = focused; visual is not null && !ReferenceEquals(visual, this); visual = visual.GetVisualParent())
            if (visual is Control control && order.Contains(control))
                return control;
        return null;
    }

    private static bool CanTakeTheFocus(Control control) =>
        control is { Focusable: true, IsEffectivelyEnabled: true, IsEffectivelyVisible: true } && KeyboardNavigation.GetIsTabStop(control);
}

// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.IO.Abstractions;
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
/// Every component of the form that <see cref="VBControls.Create(ComponentInstance, IFileSystem)"/> makes a control for is shown, in its container's canvas. A
/// component it makes no control for, such as a menu item, is not shown here, and neither is anything placed in one: a form's window shows its menus
/// on its <see cref="VBFormWindow.MenuBar"/>. A Timer's control is on the form and draws nothing.
/// </para>
/// <para>
/// A control's <c>Left</c>, <c>Top</c>, <c>Width</c> and <c>Height</c> are pixels whatever its container's <c>ScaleMode</c>, and
/// place it as they are. In each container the first control the form holds is on top until a <c>ZOrder</c> call moves one, and a
/// windowless control (a Label or an Image) is under every windowed one, whatever their order.
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
/// <para>
/// The root's drawing statements draw on its <see cref="Drawing"/>, under every control.
/// </para>
/// <para>
/// A press on no control is the root's: it raises MouseDown, MouseMove and MouseUp with <c>X</c> and <c>Y</c> in the root's scale, and
/// Click after MouseUp, for every button, where the release is over the root again and over none of its controls. A double click's
/// second press raises DblClick in place of its MouseDown, and its release raises MouseUp and no Click.
/// </para>
/// </remarks>
public sealed class VBFormView : Canvas
{
    // The windowless plane's top: a container would need a million windowed controls to reach it.
    private const int WindowlessTop = -1_000_000;

    static VBFormView()
    {
        ControlEvents.AttachMouse<VBFormView>(DblClick.InPlaceOfMouseDown, inScale: true);
        ControlEvents.AttachClick<VBFormView>(ClickRelease.OverTheControl);
    }

    private readonly IFileSystem fileSystem;
    private readonly Dictionary<ComponentInstance, Control> controls = new(ReferenceEqualityComparer.Instance);
    // The shown components, in the order the form holds them.
    private readonly List<ComponentInstance> shown = [];

    /// <summary>
    /// Creates the client area of a form.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Its file lists show the machine's file system, from the process's current directory.
    /// </remarks>
    public VBFormView(FormDefinition form)
        : this(form, new FileSystem())
    {
    }

    /// <summary>
    /// Creates the client area of a form, with the file system its file lists show.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <param name="fileSystem">The file system the form's file lists show, from its current directory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> or <paramref name="fileSystem"/> is <see langword="null"/>.</exception>
    public VBFormView(FormDefinition form, IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(fileSystem);
        this.fileSystem = fileSystem;
        Form = form;
        // What the form's drawing statements draw is under every control, the windowless ones included.
        Drawing = new VBDrawingLayer(form.Root) { ZIndex = int.MinValue };
        Children.Add(Drawing);
        Place(form.Root, this);
        ComponentBinding.Bind(this, form.Root, form.Root.Class, Apply);
        AddHandler(KeyDownEvent, OnKeyDown);
    }

    /// <summary>
    /// The form the view shows.
    /// </summary>
    public FormDefinition Form { get; }

    /// <summary>
    /// The surface the form's drawing statements draw on, under its controls.
    /// </summary>
    public VBDrawingLayer Drawing { get; }

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
            if (VBControls.Create(component, fileSystem) is not { } control)
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

    // ZOrder: the control goes to the front or the back of its plane in its container, and what it holds goes with it.
    internal void ZOrder(Control control, bool toFront)
    {
        if (control.Parent is not Panel host || VBControl.GetInstance(control) is not { } component)
            return;
        var windowless = component.Class.IsWindowless;
        var plane = host.Children
            .Where(child => !ReferenceEquals(child, control) && VBControl.GetInstance(child) is { } other
                            && controls.TryGetValue(other, out var shownAs) && ReferenceEquals(shownAs, child) && other.Class.IsWindowless == windowless)
            .OrderByDescending(child => child.ZIndex)
            .ToList();
        plane.Insert(toFront ? 0 : plane.Count, control);
        var top = windowless ? WindowlessTop : 0;
        for (var place = 0; place < plane.Count; place++)
            plane[place].ZIndex = top - place;
    }

    // The first control of the tab order that Tab could give the focus to, or none.
    internal Control? FirstToTakeTheFocus() => TabOrderOf().Select(c => controls[c]).FirstOrDefault(CanTakeTheFocus);

    internal static bool CanTakeTheFocus(Control control) =>
        control is { Focusable: true, IsEffectivelyEnabled: true, IsEffectivelyVisible: true } && KeyboardNavigation.GetIsTabStop(control);
}

// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// The component an Avalonia control shows, and the sink its events go to.
/// </summary>
/// <remarks>
/// Every control of this namespace is given its component when it is created. The sink is inherited: set on a form's window, it
/// reaches every control on the form. A control with no sink raises nothing, which is how a form shows in a designer's preview.
/// </remarks>
public static class VBControl
{
    /// <summary>
    /// The component a control shows.
    /// </summary>
    public static readonly AttachedProperty<ComponentInstance?> InstanceProperty =
        AvaloniaProperty.RegisterAttached<Control, ComponentInstance?>("Instance", typeof(VBControl));

    /// <summary>
    /// The sink a control's events go to, inherited from the control's ancestors.
    /// </summary>
    public static readonly AttachedProperty<IComponentEventSink?> EventSinkProperty =
        AvaloniaProperty.RegisterAttached<Control, IComponentEventSink?>("EventSink", typeof(VBControl), inherits: true);

    /// <summary>
    /// The clock a Timer counts its interval on, inherited from the control's ancestors.
    /// </summary>
    public static readonly AttachedProperty<TimeProvider?> TimeProviderProperty =
        AvaloniaProperty.RegisterAttached<Control, TimeProvider?>("TimeProvider", typeof(VBControl), inherits: true);

    /// <summary>
    /// Gets the component a control shows.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns>The component, or <see langword="null"/> where the control shows none.</returns>
    public static ComponentInstance? GetInstance(Control control) => control.GetValue(InstanceProperty);

    /// <summary>
    /// Sets the component a control shows.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="instance">The component.</param>
    public static void SetInstance(Control control, ComponentInstance? instance) => control.SetValue(InstanceProperty, instance);

    /// <summary>
    /// Gets the sink a control's events go to.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns>The sink, or <see langword="null"/> where the control and its ancestors have none.</returns>
    public static IComponentEventSink? GetEventSink(Control control) => control.GetValue(EventSinkProperty);

    /// <summary>
    /// Sets the sink the events of a control and of the controls it holds go to.
    /// </summary>
    /// <param name="control">The control, usually a form's window.</param>
    /// <param name="sink">The sink.</param>
    public static void SetEventSink(Control control, IComponentEventSink? sink) => control.SetValue(EventSinkProperty, sink);

    /// <summary>
    /// Gets the clock a Timer on a control counts its interval on.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <returns>The clock set on the control or its ancestors, or <see cref="TimeProvider.System"/> where none is set.</returns>
    public static TimeProvider GetTimeProvider(Control control) => control.GetValue(TimeProviderProperty) ?? TimeProvider.System;

    /// <summary>
    /// Sets the clock the Timers on a control and on the controls it holds count their intervals on.
    /// </summary>
    /// <param name="control">The control, usually a form's window.</param>
    /// <param name="clock">The clock, or <see langword="null"/> for <see cref="TimeProvider.System"/>.</param>
    public static void SetTimeProvider(Control control, TimeProvider? clock) => control.SetValue(TimeProviderProperty, clock);

    // The arguments as the handlers left them, or null where the control has no component or no sink, and nothing is raised.
    internal static FormValue[]? Raise(Control control, EventDefinition definition, params FormValue[] arguments)
    {
        if (GetInstance(control) is not { } instance || GetEventSink(control) is not { } sink)
            return null;
        sink.Raise(instance, definition, arguments);
        return arguments;
    }

    // An event of a component a part of its control raises, such as its drawing surface, to the sink the part has.
    internal static void RaiseFor(Control part, ComponentInstance instance, EventDefinition definition)
    {
        if (GetEventSink(part) is { } sink)
            sink.Raise(instance, definition, []);
    }

    // Whether control is the innermost component under the pointer. A routed pointer event bubbles, so a press on a button inside a
    // Frame reaches the Frame too, where it is not the Frame's: VB raises the innermost control's event and nothing above it. The
    // walk stops at the first visual that shows a component. A template part shows none, so a press on a Label's text is the Label's.
    internal static bool Owns(Control control, object? source)
    {
        for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (ReferenceEquals(visual, control))
                return true;
            if (visual is Control candidate && GetInstance(candidate) is not null)
                return false;
        }
        return false;
    }
}

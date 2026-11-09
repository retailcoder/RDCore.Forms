// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB scroll bar: a control that shows a <see cref="VBClasses.HScrollBar"/> or a <see cref="VBClasses.VScrollBar"/> component.
/// </summary>
/// <remarks>
/// <para>
/// The component's <c>Value</c> is the bar's: moving the thumb sets it, to a whole number, and a change to it from code moves the
/// thumb. <c>Change</c> is raised each time it changes, whichever way. Its arrows move it by <c>SmallChange</c>, and a click on its
/// track by <c>LargeChange</c>.
/// </para>
/// <para>
/// <c>Min</c> is at its left or top end and <c>Max</c> at its right or bottom end, so a <c>Min</c> greater than <c>Max</c> runs it
/// the other way.
/// </para>
/// <para>
/// It takes the focus, and raises GotFocus and LostFocus.
/// </para>
/// </remarks>
public sealed class VBScrollBar : ScrollBar
{
    private readonly ComponentInstance instance;
    private bool syncing;

    static VBScrollBar()
    {
        FocusableProperty.OverrideDefaultValue<VBScrollBar>(true);
        ControlEvents.AttachFocus<VBScrollBar>();
    }

    /// <summary>
    /// Creates a control that shows a scroll bar.
    /// </summary>
    /// <param name="instance">The HScrollBar or VScrollBar.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is neither an HScrollBar nor a VScrollBar.</exception>
    public VBScrollBar(ComponentInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (!ReferenceEquals(instance.Class, VBClasses.HScrollBar) && !ReferenceEquals(instance.Class, VBClasses.VScrollBar))
            throw new ArgumentException($"The component is a {instance.Class.TypeName}, not a scroll bar.", nameof(instance));
        this.instance = instance;
        Orientation = ReferenceEquals(instance.Class, VBClasses.HScrollBar) ? Orientation.Horizontal : Orientation.Vertical;
        AllowAutoHide = false;
        Visibility = ScrollBarVisibility.Visible;
        ComponentBinding.Bind(this, instance, instance.Class, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ScrollBar);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty && !syncing)
            instance.Set(P.ScrollBarValue, ToVB(Value));
    }

    private bool Reversed => instance.Get(P.Min) > instance.Get(P.Max);

    // Avalonia's range is ascending; a reversed bar maps each position the other way.
    private short ToVB(double value)
    {
        var min = instance.Get(P.Min);
        var max = instance.Get(P.Max);
        var position = Math.Round(Reversed ? (double)min + max - value : value, MidpointRounding.AwayFromZero);
        return (short)Math.Clamp(position, Math.Min(min, max), Math.Max(min, max));
    }

    private double ToAvalonia(short value) => Reversed ? (double)instance.Get(P.Min) + instance.Get(P.Max) - value : value;

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        // A scroll bar's Value is the ScrollBarValue property, named Value as in VB.
        if (ReferenceEquals(e.Property, P.ScrollBarValue))
        {
            Sync(() => Value = ToAvalonia((short)e.After!));
            if (!Equals(e.Before, e.After))
                VBControl.Raise(this, VBEvents.Change);
            return;
        }
        switch (e.Property.Name)
        {
            case nameof(P.Min) or nameof(P.Max):
                Sync(() =>
                {
                    Minimum = Math.Min(instance.Get(P.Min), instance.Get(P.Max));
                    Maximum = Math.Max(instance.Get(P.Min), instance.Get(P.Max));
                    Value = ToAvalonia(instance.Get(P.ScrollBarValue));
                });
                break;
            case nameof(P.SmallChange):
                SmallChange = (short)e.After!;
                break;
            case nameof(P.LargeChange):
                LargeChange = (short)e.After!;
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
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
}

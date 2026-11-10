// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB OptionButton: a <see cref="RadioButton"/> that shows a <see cref="VBClasses.OptionButton"/> component.
/// </summary>
/// <remarks>
/// <para>
/// The option buttons in one container are a group, and selecting one clears the others. The component's <c>Value</c> is the
/// button's. Setting it to False from code on the selected button leaves the group with none selected. <c>Click</c> is raised as a
/// button becomes selected, whichever way: by a click, by the focus, or by code. Clearing a button, and selecting the selected one
/// again, raises none.
/// </para>
/// <para>
/// A button that takes the focus by the keyboard or from code is selected before its GotFocus, and its Click comes after the GotFocus.
/// A group is one tab stop: its selected button, or each of them where none is selected. An arrow key moves the focus to the previous
/// or next control in the container, which selects a button it lands on, and raises no key event. It raises GotFocus, LostFocus,
/// KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp. Space on the selected button raises its key events and no Click. A double
/// click's second press raises MouseDown, then DblClick. The whole rectangle takes the pointer, as a VB OptionButton's window does,
/// whatever part of it the theme draws.
/// </para>
/// </remarks>
public sealed class VBOptionButton : RadioButton, ICustomHitTest, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private bool showing;
    // Set while the focus selects it, until its GotFocus has been raised.
    private bool clickAfterFocus;
    // The TabStop of the component, apart from the stop the group allows.
    private bool tabStop = true;
    private bool settingTabStop;

    static VBOptionButton()
    {
        ControlKeys.Attach<VBOptionButton>(arrowsMoveTheFocus: true);
        ControlEvents.AttachMouse<VBOptionButton>(DblClick.AfterMouseDown);
        // Before the GotFocus raised: the focus by the keyboard or from code selects the button. A press selects it on its own path.
        GotFocusEvent.AddClassHandler<VBOptionButton>((option, e) =>
        {
            if (!ReferenceEquals(e.Source, option) || e is not FocusChangedEventArgs { NavigationMethod: not NavigationMethod.Pointer }
                || option.IsChecked == true)
                return;
            option.clickAfterFocus = true;
            option.IsChecked = true;
        }, handledEventsToo: true);
        ControlEvents.AttachFocus<VBOptionButton>();
        ArrowNavigation.Attach<VBOptionButton>();
        // After the GotFocus raised.
        GotFocusEvent.AddClassHandler<VBOptionButton>((option, _) =>
        {
            if (!option.clickAfterFocus)
                return;
            option.clickAfterFocus = false;
            VBControl.Raise(option, VBEvents.Click);
        }, handledEventsToo: true);
        KeyboardNavigation.IsTabStopProperty.Changed.AddClassHandler<VBOptionButton>((option, e) =>
        {
            if (option.settingTabStop)
                return;
            option.tabStop = e.GetNewValue<bool>();
            option.UpdateGroupTabStops();
        });
    }

    /// <summary>
    /// Creates a button that shows an OptionButton.
    /// </summary>
    /// <param name="instance">The OptionButton.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not an OptionButton.</exception>
    public VBOptionButton(ComponentInstance instance)
    {
        this.instance = instance;
        ComponentBinding.Bind(this, instance, VBClasses.OptionButton, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(RadioButton);

    /// <inheritdoc/>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    // The other option buttons of its container.
    private IEnumerable<VBOptionButton> Group => (Parent as Panel)?.Children.OfType<VBOptionButton>() ?? [this];

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsCheckedProperty)
            return;
        // A selection by a click, by the focus or by the group clearing it goes to the component, which raises Click from it.
        if (!showing)
            instance.Set(P.OptionButtonValue, IsChecked == true);
        // Selecting it makes it its group's stop again, whatever TabStop it had.
        if (IsChecked == true)
            tabStop = true;
        UpdateGroupTabStops();
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateGroupTabStops();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // The group it leaves may have lost its selected button, which makes the others tab stops again.
        var group = Group.Where(o => !ReferenceEquals(o, this)).ToList();
        base.OnDetachedFromVisualTree(e);
        if (group.Count > 0)
            group[0].UpdateGroupTabStops();
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.OptionButtonValue))
        {
            var selected = (bool)e.After!;
            if (IsChecked != selected)
            {
                showing = true;
                try
                {
                    IsChecked = selected;
                }
                finally
                {
                    showing = false;
                }
                // Selecting it from code clears the button it replaces, which tells its own component.
                if (selected)
                    foreach (var other in Group.Where(o => !ReferenceEquals(o, this) && o.instance.Get(P.OptionButtonValue)))
                        other.instance.Set(P.OptionButtonValue, false);
            }
            if (selected && !Equals(e.Before, e.After) && !clickAfterFocus)
                VBControl.Raise(this, VBEvents.Click);
        }
        else if (ReferenceEquals(e.Property, P.Caption))
            VBCaption.ShowIn(this, (string)e.After!);
        else if (ReferenceEquals(e.Property, P.TabStop))
        {
            tabStop = (bool)e.After!;
            UpdateGroupTabStops();
        }
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }

    private void UpdateGroupTabStops()
    {
        var group = Group.ToList();
        var anySelected = group.Any(o => o.IsChecked == true);
        foreach (var option in group)
            option.ApplyTabStop(option.IsChecked == true || !anySelected);
    }

    private void ApplyTabStop(bool groupStop)
    {
        settingTabStop = true;
        try
        {
            KeyboardNavigation.SetIsTabStop(this, tabStop && groupStop);
        }
        finally
        {
            settingTabStop = false;
        }
    }
}

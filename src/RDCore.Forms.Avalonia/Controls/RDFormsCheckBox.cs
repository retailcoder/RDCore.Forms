// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB CheckBox: a <see cref="CheckBox"/> that shows a <see cref="ComponentClasses.CheckBox"/> component.
/// </summary>
/// <remarks>
/// <para>
/// The component's <c>Value</c> is the box's: a click sets it, and a change to it from code shows in the box. A click on a checked
/// or a grayed box clears it, and on a cleared one checks it. <c>Click</c> is raised from the component's <c>Value</c>, whichever
/// way it changed, and only when it changed.
/// </para>
/// <para>
/// It raises GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp, Click before MouseUp. Space raises
/// KeyDown, KeyPress and KeyUp, then Click. An arrow key moves the focus to the previous or next control in its container, changes no
/// Value, and raises no key event. The whole rectangle takes the pointer, as a VB CheckBox's window does, whatever part of it the theme
/// draws.
/// </para>
/// <para>
/// Its <c>Style</c> is read as it is made, and a later change is ignored. A Standard box does not draw its <c>Picture</c>. A Graphical
/// box is a push button, drawn as a <see cref="ToggleButton"/>, with the face of a graphical
/// <see cref="RDFormsCommandButton"/>; it is pushed in, and shows its <c>DownPicture</c>, while it is pressed or checked. A grayed box is
/// drawn as a cleared one.
/// </para>
/// </remarks>
public sealed class RDFormsCheckBox : CheckBox, ICustomHitTest, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private readonly GraphicalFace? face;
    private bool showing;

    static RDFormsCheckBox()
    {
        ControlKeys.Attach<RDFormsCheckBox>(arrowsMoveTheFocus: true);
        ControlEvents.AttachMouse<RDFormsCheckBox>();
        ControlEvents.AttachFocus<RDFormsCheckBox>();
        ArrowNavigation.Attach<RDFormsCheckBox>();
    }

    /// <summary>
    /// Creates a box that shows a CheckBox.
    /// </summary>
    /// <param name="instance">The CheckBox.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a CheckBox.</exception>
    public RDFormsCheckBox(ComponentInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        this.instance = instance;
        if (instance.Get(P.ButtonStyle) == ButtonStyle.Graphical)
            GraphicalFace.ShowIn(this, face = new GraphicalFace());
        ComponentBinding.Bind(this, instance, ComponentClasses.CheckBox, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => face is null ? typeof(CheckBox) : typeof(ToggleButton);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (face is not null && (change.Property == IsPressedProperty || change.Property == IsCheckedProperty))
            face.IsDown = IsPressed || IsChecked == true;
    }

    /// <inheritdoc/>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    /// <inheritdoc/>
    protected override void OnClick()
    {
        // The press toggles the box; the component takes the new state, and raises Click from it.
        base.OnClick();
        if (!showing)
            instance.Set(P.CheckBoxValue, IsChecked switch
            {
                true => CheckBoxValue.Checked,
                null => CheckBoxValue.Grayed,
                _ => CheckBoxValue.Unchecked,
            });
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.CheckBoxValue))
        {
            var value = (CheckBoxValue)e.After!;
            showing = true;
            try
            {
                // Grayed only from code or the designer: a click never grays a box.
                IsThreeState = value == CheckBoxValue.Grayed;
                IsChecked = value switch
                {
                    CheckBoxValue.Checked => true,
                    CheckBoxValue.Grayed => null,
                    _ => false,
                };
            }
            finally
            {
                showing = false;
            }
            if (!Equals(e.Before, e.After))
                RDFormsControl.Raise(this, Events.Click);
        }
        else if (ReferenceEquals(e.Property, P.Caption))
            GraphicalFace.ShowCaption(this, face, (string)e.After!);
        else if (GraphicalFace.IsDrawnByFace(e.Property))
            face?.Take(e.Property, e.After);
        else if (!ReferenceEquals(e.Property, P.ButtonStyle))
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }
}

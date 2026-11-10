// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB CheckBox: a <see cref="CheckBox"/> that shows a <see cref="VBClasses.CheckBox"/> component.
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
/// </remarks>
public sealed class VBCheckBox : CheckBox, ICustomHitTest, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private bool showing;

    static VBCheckBox()
    {
        ControlKeys.Attach<VBCheckBox>(arrowsMoveTheFocus: true);
        ControlEvents.AttachMouse<VBCheckBox>();
        ControlEvents.AttachFocus<VBCheckBox>();
        ArrowNavigation.Attach<VBCheckBox>();
    }

    /// <summary>
    /// Creates a box that shows a CheckBox.
    /// </summary>
    /// <param name="instance">The CheckBox.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a CheckBox.</exception>
    public VBCheckBox(ComponentInstance instance)
    {
        this.instance = instance;
        ComponentBinding.Bind(this, instance, VBClasses.CheckBox, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(CheckBox);

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
                VBControl.Raise(this, VBEvents.Click);
        }
        else if (ReferenceEquals(e.Property, P.Caption))
            VBCaption.ShowIn(this, (string)e.After!);
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }
}

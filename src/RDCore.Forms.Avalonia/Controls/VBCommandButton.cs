// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB CommandButton: a <see cref="Button"/> that shows a <see cref="VBClasses.CommandButton"/> component.
/// </summary>
/// <remarks>
/// It raises Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp. Click comes before MouseUp, as in
/// VB, and Space raises KeyDown, KeyPress and KeyUp, then Click. An arrow key moves the focus to the previous or next control in its
/// container, and raises no key event. Its <c>Picture</c> is not drawn.
/// </remarks>
public sealed class VBCommandButton : Button, IRaisesKeyEvents
{
    static VBCommandButton()
    {
        ControlKeys.Attach<VBCommandButton>(arrowsMoveTheFocus: true);
        ControlEvents.AttachFocus<VBCommandButton>();
        ArrowNavigation.Attach<VBCommandButton>();
        // Avalonia's Button raises Click in its own release handler, registered first, so MouseUp follows it.
        ClickEvent.AddClassHandler<VBCommandButton>((button, _) => VBControl.Raise(button, VBEvents.Click));
        ControlEvents.AttachMouse<VBCommandButton>();
    }

    /// <summary>
    /// Creates a button that shows a CommandButton.
    /// </summary>
    /// <param name="instance">The CommandButton.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a CommandButton.</exception>
    public VBCommandButton(ComponentInstance instance) => ComponentBinding.Bind(this, instance, VBClasses.CommandButton, Apply);

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Button);

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.Caption))
            VBCaption.ShowIn(this, (string)e.After!);
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }
}

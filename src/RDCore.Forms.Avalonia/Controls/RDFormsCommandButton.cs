// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Styling;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB CommandButton: a <see cref="Button"/> that shows a <see cref="ComponentClasses.CommandButton"/> component.
/// </summary>
/// <remarks>
/// <para>
/// It raises Click, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp. Click comes before MouseUp, as in
/// VB, and Space raises KeyDown, KeyPress and KeyUp, then Click. An arrow key moves the focus to the previous or next control in its
/// container, and raises no key event. Enter on the button clicks it, with no key event, and its <c>Default</c> and <c>Cancel</c> make it
/// the button that Enter and Esc click from the form's other controls (<see cref="Rendering.RDFormsFormWindow"/>). On a form's window, the button
/// that Enter clicks has the default look: a border two pixels wide in the system's <c>Highlight</c> colour, and the <c>:default</c>
/// pseudo-class. Its <c>Value</c> set to True clicks it, shown or not, enabled or not (<see cref="Properties.CommandButtonValue"/>).
/// </para>
/// <para>
/// Its <c>Style</c> is read as it is made, and a later change is ignored. A Standard button shows its caption only: its <c>Picture</c>
/// is not drawn, and its <c>BackColor</c> is not painted: its face is the system's <c>ButtonFace</c> colour, pressed or not, whatever
/// the theme's variant. A Graphical button paints its <c>BackColor</c>, and shows its caption at the
/// bottom and its picture centred above it, both one pixel right and down while it is pressed. While pressed it shows its
/// <c>DownPicture</c>, and while disabled its <c>DisabledPicture</c>, where it has one; a disabled button with no
/// <c>DisabledPicture</c> shows its <c>Picture</c> greyed. Its caption is in the system's <c>ButtonText</c> colour: a CommandButton has
/// no <c>ForeColor</c>.
/// </para>
/// </remarks>
public sealed class RDFormsCommandButton : Button, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private readonly GraphicalFace? face;

    static RDFormsCommandButton()
    {
        ControlKeys.Attach<RDFormsCommandButton>(arrowsMoveTheFocus: true);
        ControlEvents.AttachFocus<RDFormsCommandButton>();
        ArrowNavigation.Attach<RDFormsCommandButton>();
        // Avalonia's Button raises Click in its own release handler, registered first, so MouseUp follows it.
        ClickEvent.AddClassHandler<RDFormsCommandButton>((button, _) => RDFormsControl.Raise(button, Events.Click));
        ControlEvents.AttachMouse<RDFormsCommandButton>();
    }

    /// <summary>
    /// Creates a button that shows a CommandButton.
    /// </summary>
    /// <param name="instance">The CommandButton.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a CommandButton.</exception>
    public RDFormsCommandButton(ComponentInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        this.instance = instance;
        if (instance.Get(P.ButtonStyle) == ButtonStyle.Graphical)
            GraphicalFace.ShowIn(this, face = new GraphicalFace());
        else
            ShowSystemFace(this);
        // A CommandButton has no ForeColor: Windows draws its caption in the button text colour.
        Foreground = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.ButtonText));
        ComponentBinding.Bind(this, instance, ComponentClasses.CommandButton, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Button);

    // A Standard button's face is the system's button face colour, pressed or not, as its caption is the button text colour: both are
    // the form's palette, never the theme's, whose dark variant would put the black caption on a dark face.
    private static void ShowSystemFace(RDFormsCommandButton button)
    {
        var face = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.ButtonFace));
        button.Background = face;
        // the button's style key is Button's, which a selector matches on.
        button.Styles.Add(new Style(x => x.OfType<Button>().Class(":pressed").Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"))
        {
            Setters = { new Setter(ContentPresenter.BackgroundProperty, face) },
        });
    }

    // The default look, which the form's window gives the button that Enter clicks.
    internal void ShowAsDefault(bool isDefault)
    {
        if (PseudoClasses.Contains(":default") == isDefault)
            return;
        PseudoClasses.Set(":default", isDefault);
        if (isDefault)
        {
            BorderBrush = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.Highlight));
            BorderThickness = new Thickness(2);
        }
        else
        {
            ClearValue(BorderBrushProperty);
            ClearValue(BorderThicknessProperty);
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (face is not null && change.Property == IsPressedProperty)
            face.IsDown = IsPressed;
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.CommandButtonValue))
        {
            // True clicks the button, whether it is shown, enabled or neither, with no focus and no mouse event. Click reads True,
            // and the Value is False again after it: unset, so that a form's file holds none.
            if (e.After is true)
            {
                RDFormsControl.Raise(this, Events.Click);
                instance.Reset(P.CommandButtonValue);
            }
        }
        else if (ReferenceEquals(e.Property, P.Caption))
            GraphicalFace.ShowCaption(this, face, (string)e.After!);
        else if (GraphicalFace.IsDrawnByFace(e.Property))
            face?.Take(e.Property, e.After);
        else if (ReferenceEquals(e.Property, P.ButtonStyle) || face is null && ReferenceEquals(e.Property, P.BackColor))
        {
            // The style is kept as the button was made, and a Standard button does not paint its BackColor.
        }
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }
}

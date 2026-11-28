using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Rendering;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// An RDForms ToggleButton: a <see cref="ToggleButton"/> that shows a <see cref="ComponentClasses.ToggleButton"/> component.
/// </summary>
/// <remarks>
/// <para>
/// The component's <c>Value</c> is the button's: a click sets it, and a change to it from code or the designer shows in the button. It
/// is down while its <c>Value</c> is True, up while it is False, and drawn up where it is Null. A click on an up button puts it down. A
/// click on a down one puts it up, or makes it Null where its <c>TripleState</c> is True, and a click on a Null one puts it up. A
/// <c>Locked</c> button keeps its <c>Value</c> through a click.
/// </para>
/// <para>
/// Change, then Click, is raised from the component's <c>Value</c>, whichever way it changed, and only when it changed. It raises
/// GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove and MouseUp, and a double click's second press raises
/// MouseDown, then DblClick. Space raises KeyDown, KeyPress and KeyUp, then the click. An arrow key moves the focus to the previous or
/// next control in its container, and raises no key event. The whole rectangle takes the pointer, whatever part of it the theme
/// draws.
/// </para>
/// <para>
/// It shows its <c>Caption</c>, with its access key. With a <c>Picture</c>, it lays the picture and the caption out as an MSForms
/// ToggleButton does (measured in Excel): one block, centred inside the button 4 pixels from its edge, the picture
/// where its <c>PicturePosition</c> puts it, one pixel from the caption, and shrunk to fit. The caption takes 4 pixels more across and
/// down than its text. A bitmap's lower-left pixel is transparent. While the button is down, both are drawn one pixel right and down.
/// </para>
/// </remarks>
public sealed class RDFormsToggleButton : ToggleButton, ICustomHitTest, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private readonly GraphicalFace face = new() { Position = PicturePosition.AboveCenter, MasksLowerLeft = true };

    static RDFormsToggleButton()
    {
        ControlKeys.Attach<RDFormsToggleButton>(arrowsMoveTheFocus: true);
        ControlEvents.AttachMouse<RDFormsToggleButton>(DblClick.AfterMouseDown);
        ControlEvents.AttachFocus<RDFormsToggleButton>();
        ArrowNavigation.Attach<RDFormsToggleButton>();
    }

    /// <summary>
    /// Creates a button that shows a ToggleButton.
    /// </summary>
    /// <param name="instance">The ToggleButton.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a ToggleButton.</exception>
    public RDFormsToggleButton(ComponentInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        this.instance = instance;
        ComponentBinding.Bind(this, instance, ComponentClasses.ToggleButton, Apply);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ToggleButton);

    /// <inheritdoc/>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    /// <summary>
    /// Where the button draws its picture, from its top left corner, or <see langword="null"/> where it draws none.
    /// </summary>
    public Rect? PictureBounds => face.PictureBoundsIn(this);

    /// <summary>
    /// Where the button draws its caption's text, from its top left corner, or <see langword="null"/> where its picture is not shown with it.
    /// </summary>
    public Rect? CaptionBounds => face.CaptionBoundsIn(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty || change.Property == IsPressedProperty)
            face.IsDown = IsChecked == true || IsPressed;
    }

    /// <inheritdoc/>
    protected override void OnClick()
    {
        // The component takes the state the click leads to, and the button shows it: it does not toggle itself.
        if (instance.Get(P.Locked))
            return;
        instance.Set(P.ToggleButtonValue, instance.Get(P.ToggleButtonValue) switch
        {
            false => true,
            true => instance.Get(P.TripleState) ? null : false,
            null => (bool?)false,
        });
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.ToggleButtonValue))
        {
            // A Null button is drawn up.
            IsChecked = (bool?)e.After == true;
            if (!Equals(e.Before, e.After))
            {
                RDFormsControl.Raise(this, Events.Change);
                RDFormsControl.Raise(this, Events.Click);
            }
        }
        else if (ReferenceEquals(e.Property, P.Caption) || ReferenceEquals(e.Property, P.Picture))
        {
            if (ReferenceEquals(e.Property, P.Picture))
                face.Take(P.Picture, e.After);
            ShowContent();
        }
        else if (ReferenceEquals(e.Property, P.PicturePosition))
            face.Position = (PicturePosition)e.After!;
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }

    // The caption alone as the theme shows it, or, with a picture, the face that lays the two out.
    private void ShowContent()
    {
        var caption = instance.Get(P.Caption);
        if (face.HasPicture)
        {
            if (!ReferenceEquals(Content, face))
            {
                // The caption's template makes controls of strings only.
                ClearValue(ContentTemplateProperty);
                GraphicalFace.ShowIn(this, face);
            }
            GraphicalFace.ShowCaption(this, face, caption);
            return;
        }
        if (ReferenceEquals(Content, face))
        {
            Content = null;
            ClearValue(OpacityProperty);
            ClearValue(PaddingProperty);
            ClearValue(HorizontalContentAlignmentProperty);
            ClearValue(VerticalContentAlignmentProperty);
        }
        RDFormsCaption.ShowIn(this, caption);
    }
}

// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB PictureBox: a control that shows a <see cref="VBClasses.PictureBox"/> component, and holds the controls placed in it.
/// </summary>
/// <remarks>
/// <para>
/// Its border is a sunken edge when 3-D and one pixel when flat. The controls it holds are placed inside the border, from its client
/// origin. Its <c>Picture</c> is drawn at its own size in pixels from the client origin, clipped, under the controls it holds. A
/// metafile is not drawn. Neither is a picture <see cref="VBPicture.Decode"/> refuses, and <see cref="PictureError"/> says why. Its
/// drawing statements draw on its <see cref="Drawing"/>, over its picture and under its controls.
/// </para>
/// <para>
/// Where <c>AutoSize</c> is set, it takes its picture's size inside its border as its <c>Picture</c> changes or its <c>AutoSize</c>
/// is set. It keeps its size where its <c>Picture</c> is cleared or its <c>AutoSize</c> is cleared, and a size set by code also when
/// the same picture is set again.
/// </para>
/// <para>
/// It raises Resize inside the change, as its size in whole pixels or its border's width changes after it is made: one for a
/// <c>Move</c> or a picture that <c>AutoSize</c> fits, which set both sizes. A change of less than a pixel, the same size, a position, its
/// <c>Visible</c> and its scale raise none. It raises Resize whether or not it or its form is shown.
/// </para>
/// <para>
/// It takes the focus, whether or not it holds controls. It raises Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove and
/// MouseUp, MouseUp before Click; Click only where the release is over the PictureBox again. A press on a control it holds is that
/// control's. Its mouse events' X and Y are in twips.
/// </para>
/// <para>
/// With the focus, it raises KeyDown and KeyUp for every key, the arrow keys included, and KeyPress for a key that types a character. An
/// arrow does not move the focus. A key
/// pressed in a control it holds is that control's. These key rules were not measured with VB6: RDForms sets them.
/// </para>
/// </remarks>
public sealed class VBPictureBox : Control, IVBContainer, IRaisesKeyEvents
{
    private readonly ComponentInstance instance;
    private readonly VBEdge edge = new();
    private readonly VBPicture.Holder picture = new();
    private readonly PictureLayer layer = new();
    private IBrush background = Brushes.Transparent;
    // Its size in whole pixels and its border's width, as of its last Resize, and the changes in progress that raise one Resize between
    // them. As it is made, it is in no window yet, and the Resizes its values raise reach no handler.
    private (double Width, double Height, double Border) extent;
    private int resizing;

    static VBPictureBox()
    {
        ControlEvents.AttachMouse<VBPictureBox>(DblClick.InPlaceOfMouseDown);
        ControlEvents.AttachClick<VBPictureBox>(ClickRelease.OverTheControl);
        FocusableProperty.OverrideDefaultValue<VBPictureBox>(true);
        ControlEvents.AttachFocus<VBPictureBox>();
        ControlKeys.Attach<VBPictureBox>();
    }

    /// <summary>
    /// Creates a control that shows a PictureBox.
    /// </summary>
    /// <param name="instance">The PictureBox.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a PictureBox.</exception>
    public VBPictureBox(ComponentInstance instance)
    {
        this.instance = instance;
        VisualChildren.Add(edge);
        LogicalChildren.Add(edge);
        // What its drawing statements draw is over its picture and under the controls it holds. The layer is made first: it reads the
        // picture the binding decodes.
        Drawing = new VBDrawingLayer(instance);
        ComponentBinding.Bind(this, instance, VBClasses.PictureBox, Apply);
        edge.Child = new Panel { Children = { layer, Drawing, ChildHost } };
    }

    /// <inheritdoc/>
    public Canvas ChildHost { get; } = new();

    /// <summary>
    /// The surface its drawing statements draw on.
    /// </summary>
    public VBDrawingLayer Drawing { get; }

    /// <summary>
    /// The edge its border is drawn with.
    /// </summary>
    public VBEdgeStyle Edge => edge.Edge;

    /// <summary>
    /// The size in pixels of the picture it draws, or <see langword="null"/> where it draws none.
    /// </summary>
    public PixelSize? PictureSize => picture.Picture?.PixelSize;

    /// <summary>
    /// <see cref="FormError.InvalidPicture"/> where its component's <c>Picture</c> is refused and not drawn; otherwise
    /// <see langword="null"/>.
    /// </summary>
    public FormError? PictureError => picture.Error;

    /// <inheritdoc/>
    public override void Render(DrawingContext context) => context.FillRectangle(background, new Rect(Bounds.Size));

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.BorderStyle) or nameof(P.Appearance):
                edge.Edge = VBEdge.Of(instance.Get(P.BorderStyle), instance.Get(P.Appearance));
                Resized();
                break;
            case nameof(P.Width) or nameof(P.Height):
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                Resized();
                break;
            case nameof(P.AutoSize):
                if (!Equals(e.Before, e.After))
                    FitToPicture();
                break;
            case nameof(P.BackColor):
                background = ComponentBinding.Brush(e.After);
                InvalidateVisual();
                break;
            case nameof(P.ForeColor):
                SetValue(TextElement.ForegroundProperty, ComponentBinding.Brush(e.After));
                break;
            case nameof(P.Picture):
                if (picture.Take((byte[]?)e.After))
                {
                    layer.Picture = picture.Picture;
                    Drawing.Picture = picture.Picture;
                    FitToPicture();
                }
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }

    // A PictureBox whose AutoSize is set takes its picture's size inside its border, as its picture changes or its AutoSize is set.
    // It keeps its size where it has no picture. It reads AutoSize, BorderStyle and Appearance from its component, whatever order
    // they are applied in.
    private void FitToPicture()
    {
        if (!instance.Get(P.AutoSize) || picture.Picture is not { } shown)
            return;
        var border = 2 * VBEdge.WidthOf(VBEdge.Of(instance.Get(P.BorderStyle), instance.Get(P.Appearance)));
        Resizing(() => instance.Set(P.Width, shown.PixelSize.Width + border).Set(P.Height, shown.PixelSize.Height + border));
    }

    // Makes the changes, a Move's or a fit's, and raises one Resize at most for them all.
    internal void Resizing(Action change)
    {
        resizing++;
        try
        {
            change();
        }
        finally
        {
            resizing--;
        }
        Resized();
    }

    // Raises Resize where its extent differs from its last one: a value applied again raises nothing.
    private void Resized()
    {
        if (resizing > 0)
            return;
        var now = Extent();
        if (now == extent)
            return;
        extent = now;
        VBControl.Raise(this, VBEvents.Resize);
    }

    // VB6 raises Resize as the window's size in whole pixels, or its client area, changes: part of a pixel changes neither.
    private (double, double, double) Extent() =>
        (Math.Round(instance.Get(P.Width), MidpointRounding.AwayFromZero), Math.Round(instance.Get(P.Height), MidpointRounding.AwayFromZero),
         VBEdge.WidthOf(edge.Edge));
}

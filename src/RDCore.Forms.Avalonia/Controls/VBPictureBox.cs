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
/// metafile is not drawn. Neither is a picture <see cref="VBPicture.Decode"/> refuses, and <see cref="PictureError"/> says why.
/// </para>
/// <para>
/// It takes the focus, whether or not it holds controls. It raises Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove and
/// MouseUp, MouseUp before Click; Click only where the release is over the PictureBox again. A press on a control it holds is that
/// control's. Its mouse events' X and Y are in twips.
/// </para>
/// </remarks>
public sealed class VBPictureBox : Control, IVBContainer
{
    private readonly ComponentInstance instance;
    private readonly VBEdge edge = new();
    private readonly VBPicture.Holder picture = new();
    private readonly PictureLayer layer = new();
    private IBrush background = Brushes.Transparent;

    static VBPictureBox()
    {
        ControlEvents.AttachMouse<VBPictureBox>(DblClick.InPlaceOfMouseDown);
        ControlEvents.AttachClick<VBPictureBox>(ClickRelease.OverTheControl);
        FocusableProperty.OverrideDefaultValue<VBPictureBox>(true);
        ControlEvents.AttachFocus<VBPictureBox>();
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
        edge.Child = new Panel { Children = { layer, ChildHost } };
        VisualChildren.Add(edge);
        LogicalChildren.Add(edge);
        ComponentBinding.Bind(this, instance, VBClasses.PictureBox, Apply);
    }

    /// <inheritdoc/>
    public Canvas ChildHost { get; } = new();

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
                    layer.Picture = picture.Picture;
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }
}

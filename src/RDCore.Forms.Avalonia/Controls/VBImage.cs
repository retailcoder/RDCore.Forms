// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Image: a control that shows a <see cref="VBClasses.Image"/> component.
/// </summary>
/// <remarks>
/// <para>
/// Its single border is a sunken edge. Its <c>Picture</c> is drawn inside its border: stretched over the whole of it where
/// <c>Stretch</c> is set, and otherwise at its own size in pixels from the top left corner, clipped. A metafile
/// is not drawn. Neither is a picture <see cref="VBPicture.Decode"/> refuses, and <see cref="PictureError"/> says why.
/// </para>
/// <para>
/// Where <c>Stretch</c> is not set, it takes its picture's size inside its border as its <c>Picture</c> changes or its
/// <c>Stretch</c> is cleared, and its <c>Left</c> and <c>Top</c> go to whole pixels. It keeps its size where its <c>Picture</c> is
/// cleared, and a size set by code also when the same picture is set again.
/// </para>
/// <para>
/// It takes no focus. It raises Click, DblClick, MouseDown, MouseMove and MouseUp, MouseUp before Click; Click only where the release
/// is over the image again. The whole rectangle takes the pointer, where it draws nothing too.
/// </para>
/// </remarks>
public sealed class VBImage : Control, ICustomHitTest
{
    private readonly ComponentInstance instance;
    private readonly VBEdge edge = new();
    private readonly VBPicture.Holder picture = new();
    private bool stretch;

    static VBImage()
    {
        ControlEvents.AttachMouse<VBImage>(DblClick.InPlaceOfMouseDown);
        ControlEvents.AttachClick<VBImage>(ClickRelease.OverTheControl);
    }

    /// <summary>
    /// Creates a control that shows an Image.
    /// </summary>
    /// <param name="instance">The Image.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not an Image.</exception>
    public VBImage(ComponentInstance instance)
    {
        this.instance = instance;
        VisualChildren.Add(edge);
        LogicalChildren.Add(edge);
        ComponentBinding.Bind(this, instance, VBClasses.Image, Apply);
    }

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
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var inside = new Rect(Bounds.Size).Deflate(VBEdge.WidthOf(edge.Edge));
        using (context.PushClip(inside))
        using (context.PushTransform(Matrix.CreateTranslation(inside.X, inside.Y)))
            VBPicture.Draw(context, picture.Picture, inside.Size, stretch);
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.BorderStyle):
                edge.Edge = VBEdge.Of((BorderStyle)e.After!, Appearance.ThreeD);
                InvalidateVisual();
                break;
            case nameof(P.Picture):
                if (picture.Take((byte[]?)e.After))
                {
                    InvalidateVisual();
                    FitToPicture();
                }
                break;
            case nameof(P.Stretch):
                stretch = (bool)e.After!;
                InvalidateVisual();
                if (!Equals(e.Before, e.After))
                    FitToPicture();
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }

    // An Image whose Stretch is not set takes its picture's size, inside its border, as its picture changes or its Stretch is cleared,
    // and its Left and Top go to whole pixels. It keeps its size where it has no picture. It reads Stretch and BorderStyle from its
    // component, whatever order they are applied in.
    private void FitToPicture()
    {
        if (instance.Get(P.Stretch) || picture.Picture is not { } shown)
            return;
        var border = 2 * VBEdge.WidthOf(VBEdge.Of(instance.Get(P.BorderStyle), Appearance.ThreeD));
        instance.Set(P.Left, Math.Round(instance.Get(P.Left))).Set(P.Top, Math.Round(instance.Get(P.Top)))
            .Set(P.Width, shown.PixelSize.Width + border).Set(P.Height, shown.PixelSize.Height + border);
    }
}

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
/// A VB Image: a control that shows a <see cref="VBClasses.Image"/> component.
/// </summary>
/// <remarks>
/// <para>
/// Its single border is a sunken edge. Its <c>Picture</c> is not drawn.
/// </para>
/// <para>
/// It takes no focus. It raises Click, DblClick, MouseDown, MouseMove and MouseUp, MouseUp before Click; Click only where the release
/// is over the image again. The whole rectangle takes the pointer, where it draws nothing too.
/// </para>
/// </remarks>
public sealed class VBImage : Control, ICustomHitTest
{
    private readonly VBEdge edge = new();

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
        VisualChildren.Add(edge);
        LogicalChildren.Add(edge);
        ComponentBinding.Bind(this, instance, VBClasses.Image, Apply);
    }

    /// <summary>
    /// The edge its border is drawn with.
    /// </summary>
    public VBEdgeStyle Edge => edge.Edge;

    /// <inheritdoc/>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (ReferenceEquals(e.Property, P.BorderStyle))
            edge.Edge = VBEdge.Of((BorderStyle)e.After!, Appearance.ThreeD);
        else
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// The edges VB draws around its controls.
/// </summary>
public enum VBEdgeStyle
{
    /// <summary>
    /// No edge.
    /// </summary>
    None,

    /// <summary>
    /// One pixel of the window frame colour: a flat control's single border.
    /// </summary>
    Single,

    /// <summary>
    /// A sunken edge two pixels deep: a 3-D control's single border. The button shadow over the dark shadow at the top and left, and
    /// the 3-D light inside the button highlight at the bottom and right.
    /// </summary>
    Sunken,

    /// <summary>
    /// An etched line two pixels wide: a Frame's border. The button shadow, with the button highlight inside it.
    /// </summary>
    Etched,
}

/// <summary>
/// A decorator that draws one of VB's edges around its child, and insets the child by the edge's width.
/// </summary>
public sealed class VBEdge : Decorator
{
    /// <summary>
    /// The edge drawn.
    /// </summary>
    public static readonly StyledProperty<VBEdgeStyle> EdgeProperty = AvaloniaProperty.Register<VBEdge, VBEdgeStyle>(nameof(Edge));

    static VBEdge()
    {
        AffectsMeasure<VBEdge>(EdgeProperty);
        AffectsRender<VBEdge>(EdgeProperty);
        EdgeProperty.Changed.AddClassHandler<VBEdge>((edge, _) => edge.Padding = new Thickness(WidthOf(edge.Edge)));
    }

    /// <summary>
    /// The edge drawn.
    /// </summary>
    public VBEdgeStyle Edge
    {
        get => GetValue(EdgeProperty);
        set => SetValue(EdgeProperty, value);
    }

    /// <summary>
    /// How wide an edge is, in pixels: the inset of a control's client area.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <returns>0, 1 or 2.</returns>
    public static double WidthOf(VBEdgeStyle edge) => edge switch
    {
        VBEdgeStyle.Single => 1,
        VBEdgeStyle.Sunken or VBEdgeStyle.Etched => 2,
        _ => 0,
    };

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var (w, h) = (Bounds.Width, Bounds.Height);
        if (w <= 0 || h <= 0)
            return;
        switch (Edge)
        {
            case VBEdgeStyle.Single:
                Ring(context, 0, w, h, SystemColor.WindowFrame, SystemColor.WindowFrame);
                break;
            case VBEdgeStyle.Sunken:
                Ring(context, 0, w, h, SystemColor.ButtonShadow, SystemColor.ButtonHighlight);
                Ring(context, 1, w, h, SystemColor.ThreeDDarkShadow, SystemColor.ThreeDLight);
                break;
            case VBEdgeStyle.Etched:
                Ring(context, 0, w, h, SystemColor.ButtonShadow, SystemColor.ButtonHighlight);
                Ring(context, 1, w, h, SystemColor.ButtonHighlight, SystemColor.ButtonShadow);
                break;
        }
    }

    // One pixel ring, inset by `at`: its top and left in one colour, its bottom and right in another.
    private static void Ring(DrawingContext context, double at, double w, double h, SystemColor topLeft, SystemColor bottomRight)
    {
        var light = FormPalette.ToBrush(FormColor.FromSystem(topLeft));
        var dark = FormPalette.ToBrush(FormColor.FromSystem(bottomRight));
        context.FillRectangle(light, new Rect(at, at, Math.Max(0, w - 2 * at), 1));
        context.FillRectangle(light, new Rect(at, at, 1, Math.Max(0, h - 2 * at)));
        context.FillRectangle(dark, new Rect(at, h - at - 1, Math.Max(0, w - 2 * at), 1));
        context.FillRectangle(dark, new Rect(w - at - 1, at, 1, Math.Max(0, h - 2 * at)));
    }

    // The edge of a control's BorderStyle and Appearance: a single border is sunken when 3-D and one pixel when flat.
    internal static VBEdgeStyle Of(Model.VB.BorderStyle border, Model.VB.Appearance appearance) =>
        border == Model.VB.BorderStyle.None ? VBEdgeStyle.None
        : appearance == Model.VB.Appearance.Flat ? VBEdgeStyle.Single
        : VBEdgeStyle.Sunken;
}

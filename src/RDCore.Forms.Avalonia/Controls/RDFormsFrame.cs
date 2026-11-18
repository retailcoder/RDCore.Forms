// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Frame: a control that shows a <see cref="ComponentClasses.Frame"/> component, and holds the controls placed in it.
/// </summary>
/// <remarks>
/// <para>
/// An etched edge runs through the middle of the caption's line, and the caption paints its own background over it, as a group box
/// breaks its border for its text. The Frame's background covers the whole control.
/// </para>
/// <para>
/// It takes no focus. It raises Click, DblClick, MouseDown, MouseMove and MouseUp, MouseUp before Click; Click only where the release
/// is over the Frame again. A press on a control it holds is that control's.
/// </para>
/// <para>
/// The controls it holds scroll in its <see cref="Scroller"/>, by its <c>ScrollLeft</c> and <c>ScrollTop</c>, over at least its
/// <c>ScrollWidth</c> by <c>ScrollHeight</c>, and it raises Scroll. Its scroll bars are at the edges of the whole Frame.
/// </para>
/// </remarks>
public sealed class RDFormsFrame : Control, IRDFormsContainer
{
    private readonly ContentControl caption = new()
    {
        Padding = new Thickness(3, 0),
        Margin = new Thickness(6, 0, 0, 0),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };
    private IBrush background = Brushes.Transparent;

    static RDFormsFrame()
    {
        ControlEvents.AttachMouse<RDFormsFrame>(DblClick.InPlaceOfMouseDown);
        ControlEvents.AttachClick<RDFormsFrame>(ClickRelease.OverTheControl);
    }

    /// <summary>
    /// Creates a control that shows a Frame.
    /// </summary>
    /// <param name="instance">The Frame.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a Frame.</exception>
    public RDFormsFrame(ComponentInstance instance)
    {
        // The edge's top is 7 pixels down, through the middle of the caption's line, where Windows draws a group box's.
        ComponentBinding.Bind(this, instance, ComponentClasses.Frame, Apply);
        // The controls it holds scroll by its ScrollLeft and ScrollTop; the caption and the edge stay.
        Scroller = new RDFormsScroller(instance, this, ChildHost, followsSize: false);
        var panel = new Panel { Children = { new RDFormsEdge { Edge = RDFormsEdgeStyle.Etched, Margin = new Thickness(0, 7, 0, 0) }, caption, Scroller } };
        VisualChildren.Add(panel);
        LogicalChildren.Add(panel);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new RDFormsControlAutomationPeer(this, AutomationControlType.Group);

    /// <summary>
    /// The scroll viewer the controls it holds are in: its offset is the Frame's <c>ScrollLeft</c> and <c>ScrollTop</c>.
    /// </summary>
    public ScrollViewer Scroller { get; }

    /// <inheritdoc/>
    public Canvas ChildHost { get; } = new();

    /// <summary>
    /// The caption as shown, its access key marker taken out.
    /// </summary>
    public string Caption => caption.Content as string is { } text ? RDFormsCaption.AsShown(text) : "";

    /// <inheritdoc/>
    public override void Render(DrawingContext context) => context.FillRectangle(background, new Rect(Bounds.Size));

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.Caption):
                var text = (string)e.After!;
                caption.ContentTemplate = RDFormsCaption.Template;
                caption.Content = text;
                RDFormsCaption.NameFor(this, text);
                // A Frame with no caption breaks its edge for none.
                caption.IsVisible = text.Length > 0;
                break;
            case nameof(P.BackColor):
                background = ComponentBinding.Brush(this, e.After);
                caption.Background = background;
                InvalidateVisual();
                break;
            case nameof(P.ForeColor):
                SetValue(TextElement.ForegroundProperty, ComponentBinding.Brush(this, e.After));
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }
}

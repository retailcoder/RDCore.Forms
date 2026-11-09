// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A VB Label: a control that shows a <see cref="VBClasses.Label"/> component.
/// </summary>
/// <remarks>
/// <para>
/// Its caption's <c>&amp;</c> marks an access key where <c>UseMnemonic</c> is set; otherwise every <c>&amp;</c> is drawn. A
/// <c>BackStyle</c> of Transparent paints no background. A single border is a sunken edge when 3-D, and one pixel when flat.
/// </para>
/// <para>
/// It takes no focus. It raises Click, DblClick, MouseDown, MouseMove and MouseUp, MouseUp before Click; Click only where the release
/// is over the label again.
/// </para>
/// </remarks>
public sealed class VBLabel : Control
{
    private readonly ComponentInstance instance;
    private readonly VBEdge edge = new();
    private IBrush background = Brushes.Transparent;

    static VBLabel()
    {
        ControlEvents.AttachMouse<VBLabel>(DblClick.InPlaceOfMouseDown);
        ControlEvents.AttachClick<VBLabel>(ClickRelease.OverTheControl);
    }

    /// <summary>
    /// Creates a control that shows a Label.
    /// </summary>
    /// <param name="instance">The Label.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a Label.</exception>
    public VBLabel(ComponentInstance instance)
    {
        this.instance = instance;
        VisualChildren.Add(edge);
        LogicalChildren.Add(edge);
        ComponentBinding.Bind(this, instance, VBClasses.Label, Apply);
    }

    /// <summary>
    /// The text the label draws, its access key marker taken out where it has one.
    /// </summary>
    public string? DrawnText => (edge.Child as TextBlock)?.Text;

    /// <inheritdoc/>
    public override void Render(DrawingContext context) => context.FillRectangle(background, new Rect(Bounds.Size));

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case nameof(P.Caption) or nameof(P.UseMnemonic) or nameof(P.Alignment) or nameof(P.WordWrap):
                ShowCaption();
                break;
            case nameof(P.BorderStyle) or nameof(P.Appearance):
                edge.Edge = VBEdge.Of(instance.Get(P.BorderStyle), instance.Get(P.Appearance));
                break;
            case nameof(P.BackColor) or nameof(P.BackStyle):
                background = instance.Get(P.BackStyle) == BackStyle.Opaque ? FormPalette.ToBrush(instance.Get(P.BackColor)) : Brushes.Transparent;
                InvalidateVisual();
                break;
            case nameof(P.ForeColor):
                SetValue(TextElement.ForegroundProperty, FormPalette.ToBrush(instance.Get(P.ForeColor)));
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }

    // An AccessText only where UseMnemonic is set and the caption has a key one can carry; plain text otherwise, so that an
    // underscore in the caption does not become a key VB never gave it.
    private void ShowCaption()
    {
        var caption = instance.Get(P.Caption);
        var parsed = instance.Get(P.UseMnemonic) ? VBCaption.Parse(caption) : (VBCaption.Parsed?)null;
        var text = parsed is { FitsAccessText: true } ? new AccessText { Text = parsed.Value.ForAccessText }
            : new TextBlock { Text = parsed?.Shown ?? caption };
        text.TextWrapping = instance.Get(P.WordWrap) ? TextWrapping.Wrap : TextWrapping.NoWrap;
        text.TextAlignment = VBTextBox.ToAvalonia(instance.Get(P.Alignment));
        edge.Child = text;
        if ((parsed?.Shown ?? caption) is { Length: > 0 } shown)
            SetValue(global::Avalonia.Automation.AutomationProperties.NameProperty, shown);
        else
            ClearValue(global::Avalonia.Automation.AutomationProperties.NameProperty);
    }
}

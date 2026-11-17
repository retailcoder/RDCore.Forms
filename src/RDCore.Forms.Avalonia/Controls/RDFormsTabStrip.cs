using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A TabStrip: a control that shows a <see cref="ComponentClasses.TabStrip"/> component, its tabs on the side its <c>TabOrientation</c>
/// names, and an empty client area in the rest, which holds no controls.
/// </summary>
/// <remarks>
/// <para>
/// A program shows what the selected tab stands for itself, usually by placing controls over the client area: <see cref="ClientLeft"/>,
/// <see cref="ClientTop"/>, <see cref="ClientWidth"/> and <see cref="ClientHeight"/> are where it is.
/// </para>
/// <para>
/// It takes the focus, and raises Change as its <c>Value</c> changes, by a click on a tab, a key or a program; its other events name
/// the tab they are about first, as <see cref="ComponentClasses.TabStrip"/> lists them.
/// </para>
/// </remarks>
public sealed class RDFormsTabStrip : Control, IRaisesKeyEvents
{
    private readonly RDFormsTabHeaders headers = new();
    private readonly Border client = new() { BorderThickness = new Thickness(1) };
    private readonly DockPanel dock = new() { LastChildFill = true };
    private readonly ComponentInstance instance;
    private readonly TabbedInput input;

    static RDFormsTabStrip()
    {
        ControlEvents.AttachFocus<RDFormsTabStrip>();
        ControlKeys.Attach<RDFormsTabStrip>();
    }

    /// <summary>
    /// Creates a control that shows a TabStrip.
    /// </summary>
    /// <param name="instance">The TabStrip.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a TabStrip.</exception>
    public RDFormsTabStrip(ComponentInstance instance)
    {
        this.instance = instance;
        Focusable = true;
        dock.Children.Add(headers);
        dock.Children.Add(client);
        ComponentBinding.FollowVariant(this, () => client.BorderBrush = ComponentBinding.Brush(this, SystemColor.ButtonShadow));
        VisualChildren.Add(dock);
        LogicalChildren.Add(dock);
        input = new TabbedInput(this, headers, source => ReferenceEquals(ControlKeys.Owner(source), this));
        ComponentBinding.Bind(this, instance, ComponentClasses.TabStrip, Apply);
    }

    /// <summary>
    /// The captions of the tabs shown, in their order, their access key markers taken out.
    /// </summary>
    public IReadOnlyList<string> TabCaptions => headers.Captions;

    /// <summary>
    /// The left of the client area, the area the tabs leave, from the TabStrip's left, in DIP: its <c>ClientLeft</c>.
    /// </summary>
    public double ClientLeft => ClientArea.X;

    /// <summary>
    /// The top of the client area, from the TabStrip's top, in DIP: its <c>ClientTop</c>.
    /// </summary>
    public double ClientTop => ClientArea.Y;

    /// <summary>
    /// The width of the client area, in DIP: its <c>ClientWidth</c>.
    /// </summary>
    public double ClientWidth => ClientArea.Width;

    /// <summary>
    /// The height of the client area, in DIP: its <c>ClientHeight</c>.
    /// </summary>
    public double ClientHeight => ClientArea.Height;

    // Inside the client area's edge.
    private Rect ClientArea => client.TranslatePoint(default, this) is { } origin
        ? new Rect(origin, client.Bounds.Size).Deflate(client.BorderThickness)
        : default;

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        input.Pressed(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        input.Released(e);
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        input.Moved(e);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        input.KeyDown(e, instance.Get(P.TabOrientation));
    }

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case "Value":
                Refresh();
                if (!Equals(e.Before, e.After))
                    RDFormsControl.Raise(this, Events.Change);
                break;
            case nameof(P.Tabs) or nameof(P.MultiRow) or "Style" or nameof(P.TabOrientation) or nameof(P.TabFixedWidth) or nameof(P.TabFixedHeight):
                Refresh();
                break;
            case nameof(P.BackColor):
                client.Background = ComponentBinding.Brush(this, e.After);
                Refresh();
                break;
            case nameof(P.ForeColor):
                SetValue(TextElement.ForegroundProperty, ComponentBinding.Brush(this, e.After));
                break;
            default:
                ComponentBinding.ApplyCommon(this, e.Property, e.After);
                break;
        }
    }

    private void Refresh()
    {
        var orientation = instance.Get(P.TabOrientation);
        DockPanel.SetDock(headers, orientation switch
        {
            TabOrientation.Bottom => Dock.Bottom,
            TabOrientation.Left => Dock.Left,
            TabOrientation.Right => Dock.Right,
            _ => Dock.Top,
        });
        headers.Show([.. instance.Get(P.Tabs).Select(t => new TabHeader(t.Caption, t.ToolTipText, t.Enabled, t.Visible))], instance.Get(P.SelectedIndex),
            instance.Get(P.TabStyle), orientation, instance.Get(P.MultiRow), instance.Get(P.TabFixedWidth), instance.Get(P.TabFixedHeight),
            client.Background ?? Brushes.Transparent, ActualThemeVariant);
    }
}

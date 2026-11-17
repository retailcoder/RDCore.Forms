using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A MultiPage: a control that shows a <see cref="ComponentClasses.MultiPage"/> component, its pages' tabs on the side its
/// <c>TabOrientation</c> names, and the selected page, with the controls on it, in the rest.
/// </summary>
/// <remarks>
/// <para>
/// Its pages are the <see cref="RDFormsPage"/> controls it holds, in their component's order, and the tabs are theirs: a page's
/// <c>Caption</c>, <c>ToolTipText</c>, <c>Enabled</c> and <c>Visible</c> are its tab's. Only the page its <c>Value</c> selects is
/// shown, and none where <c>Value</c> selects none.
/// </para>
/// <para>
/// It takes the focus, and raises Change as its <c>Value</c> changes, by a click on a tab, a key or a program; its other events name
/// the tab they are about first, as <see cref="ComponentClasses.MultiPage"/> lists them.
/// </para>
/// </remarks>
public sealed class RDFormsMultiPage : Control, IRDFormsContainer, IRaisesKeyEvents
{
    private readonly RDFormsTabHeaders headers = new();
    private readonly Border body;
    private readonly DockPanel dock = new() { LastChildFill = true };
    private readonly ComponentInstance instance;
    private readonly TabbedInput input;
    private readonly HashSet<ComponentInstance> watched = new(ReferenceEqualityComparer.Instance);
    private IBrush background = Brushes.Transparent;

    static RDFormsMultiPage()
    {
        ControlEvents.AttachFocus<RDFormsMultiPage>();
        ControlKeys.Attach<RDFormsMultiPage>();
    }

    /// <summary>
    /// Creates a control that shows a MultiPage.
    /// </summary>
    /// <param name="instance">The MultiPage.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a MultiPage.</exception>
    public RDFormsMultiPage(ComponentInstance instance)
    {
        this.instance = instance;
        Focusable = true;
        body = new Border { BorderThickness = new Thickness(1), Child = ChildHost };
        ComponentBinding.FollowVariant(this, () => body.BorderBrush = ComponentBinding.Brush(this, SystemColor.ButtonShadow));
        dock.Children.Add(headers);
        dock.Children.Add(body);
        VisualChildren.Add(dock);
        LogicalChildren.Add(dock);
        input = new TabbedInput(this, headers, Owns);
        ChildHost.Children.CollectionChanged += (_, _) => Refresh();
        ChildHost.SizeChanged += (_, _) => SizePages();
        ComponentBinding.Bind(this, instance, ComponentClasses.MultiPage, Apply);
    }

    /// <inheritdoc/>
    public Canvas ChildHost { get; } = new() { ClipToBounds = true };

    /// <summary>
    /// The captions of the tabs shown, in their order, their access key markers taken out.
    /// </summary>
    public IReadOnlyList<string> TabCaptions => headers.Captions;

    /// <summary>
    /// The page shown, or <see langword="null"/> where none is.
    /// </summary>
    public RDFormsPage? ShownPage => ChildHost.Children.OfType<RDFormsPage>().FirstOrDefault(p => p.IsVisible);

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

    /// <inheritdoc/>
    public override void Render(DrawingContext context) => context.FillRectangle(background, new Rect(body.Bounds.Position, body.Bounds.Size));

    // A press on the MultiPage, on its tabs, or on a page's empty area is the MultiPage's.
    private bool Owns(object? source) =>
        ControlKeys.Owner(source) is { } owner && (ReferenceEquals(owner, this) || owner is RDFormsPage && ChildHost.Children.Contains(owner));

    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        switch (e.Property.Name)
        {
            case "Value":
                Refresh();
                if (!Equals(e.Before, e.After))
                    RDFormsControl.Raise(this, Events.Change);
                break;
            case nameof(P.MultiRow) or "Style" or nameof(P.TabOrientation) or nameof(P.TabFixedWidth) or nameof(P.TabFixedHeight):
                Refresh();
                break;
            case nameof(P.BackColor):
                background = ComponentBinding.Brush(this, e.After);
                Refresh();
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

    // The tabs, from the pages in their order, and the selected page shown.
    private void Refresh()
    {
        var pages = instance.Contents;
        foreach (var page in pages.Where(watched.Add))
            page.PropertyChanged += PageChanged;
        foreach (var gone in watched.Where(p => !pages.Contains(p)).ToList())
        {
            gone.PropertyChanged -= PageChanged;
            watched.Remove(gone);
        }

        var selected = instance.Get(P.SelectedIndex);
        var orientation = instance.Get(P.TabOrientation);
        DockPanel.SetDock(headers, orientation switch
        {
            TabOrientation.Bottom => Dock.Bottom,
            TabOrientation.Left => Dock.Left,
            TabOrientation.Right => Dock.Right,
            _ => Dock.Top,
        });
        headers.Show([.. pages.Select(p => new TabHeader(p.Get(P.Caption), p.Get(P.ToolTipText), p.Get(P.Enabled), p.Get(P.Visible)))], selected,
            instance.Get(P.TabStyle), orientation, instance.Get(P.MultiRow), instance.Get(P.TabFixedWidth), instance.Get(P.TabFixedHeight), background,
            ActualThemeVariant);
        foreach (var view in ChildHost.Children)
        {
            if (RDFormsControl.GetInstance(view) is { } page)
            {
                var index = IndexOf(pages, page);
                view.IsVisible = index >= 0 && index == selected && page.Get(P.Visible);
            }
        }

        SizePages();
    }

    private void PageChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        if (e.Property.Name is nameof(P.Caption) or nameof(P.ToolTipText) or nameof(P.Enabled) or nameof(P.Visible))
            Refresh();
    }

    // A page fills the area under the tabs.
    private void SizePages()
    {
        foreach (var view in ChildHost.Children)
        {
            view.Width = ChildHost.Bounds.Width;
            view.Height = ChildHost.Bounds.Height;
        }
    }

    private static int IndexOf(IReadOnlyList<ComponentInstance> pages, ComponentInstance page)
    {
        for (var index = 0; index < pages.Count; index++)
        {
            if (ReferenceEquals(pages[index], page))
                return index;
        }

        return -1;
    }
}

/// <summary>
/// A page of a MultiPage: a control that shows a <see cref="ComponentClasses.Page"/> component, and holds the controls placed on it. Its
/// <see cref="RDFormsMultiPage"/> sizes it, shows it while it is selected, and shows its caption on its tab.
/// </summary>
/// <remarks>
/// It draws no background of its own: the MultiPage's shows through. It raises no events: a press on its empty area is its MultiPage's.
/// </remarks>
public sealed class RDFormsPage : Control, IRDFormsContainer
{
    /// <summary>
    /// Creates a control that shows a page.
    /// </summary>
    /// <param name="instance">The page.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="instance"/> is not a page.</exception>
    public RDFormsPage(ComponentInstance instance)
    {
        VisualChildren.Add(ChildHost);
        LogicalChildren.Add(ChildHost);
        ComponentBinding.Bind(this, instance, ComponentClasses.Page, Apply);
    }

    /// <inheritdoc/>
    public Canvas ChildHost { get; } = new() { Background = Brushes.Transparent };

    // Its caption, tip and visibility are its tab's, which its MultiPage shows; its Enabled and its Opacity are its own.
    private void Apply(ComponentPropertyChangedEventArgs e)
    {
        if (e.Property.Name is nameof(P.Enabled) or nameof(P.Opacity))
            ComponentBinding.ApplyCommon(this, e.Property, e.After);
    }
}

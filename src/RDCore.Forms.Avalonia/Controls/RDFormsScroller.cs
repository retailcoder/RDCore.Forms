using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Avalonia.Controls;

// Scrolls a form's or a Frame's inside. Its offset is the component's ScrollLeft and ScrollTop, the area it scrolls is at least the
// component's ScrollWidth by ScrollHeight, and a bar of the component's ScrollBars shows where there is more to scroll than the inside
// shows, and always where KeepScrollBarsVisible keeps it. ScrollLeft and ScrollTop scroll the inside whether or not a bar shows, and
// so does the mouse wheel. The offset placed is the component's own, which the viewer keeps within what there is to scroll as its
// layout changes, so a ScrollTop set before the first layout scrolls the inside once it has its size.
// A program's ScrollLeft or ScrollTop that moves the inside raises Scroll at once, a PropertyChange in its direction; one past the end
// scrolls to the end, and the property keeps the value it was given. The user's scroll sets them and raises
// Scroll once it is done, with what scrolled it: a bar's arrow or the wheel a line, its track a page, its thumb an absolute change,
// Home and End the beginning and the end, and a control taking the focus a focus request.
internal sealed class RDFormsScroller : ScrollViewer
{
    private const double Tolerance = 1e-6;

    private readonly ComponentInstance instance;
    private readonly Control owner;
    private readonly Control area;
    private readonly bool followsSize;
    private ScrollBar? horizontal;
    private ScrollBar? vertical;
    private bool syncing;
    private bool posted;
    // The user's scroll since the last Scroll, and what the bars, the keys or the focus said it was.
    private Vector scrolled;
    private ScrollAction? actionX;
    private ScrollAction? actionY;

    // The scroller of a component's inside: owner raises its Scroll, area is what it scrolls, and a form's scroller takes the
    // form's Width and Height as its own, as the client area it shows.
    public RDFormsScroller(ComponentInstance instance, Control owner, Control area, bool followsSize)
    {
        this.instance = instance;
        this.owner = owner;
        this.area = area;
        this.followsSize = followsSize;
        Content = area;
        AllowAutoHide = false;
        // The tab order is the form's controls'; the scroller is none of them.
        Focusable = false;
        IsTabStop = false;
        ApplyAll();
        AttachedToLogicalTree += (_, _) =>
        {
            instance.PropertyChanged -= OnChanged;
            instance.PropertyChanged += OnChanged;
            ApplyAll();
        };
        DetachedFromLogicalTree += (_, _) => instance.PropertyChanged -= OnChanged;
        // Each press, wheel turn and key starts a scroll of its own.
        AddHandler(PointerPressedEvent, (_, _) => Forget(), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerWheelChangedEvent, (_, _) => Forget(), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(KeyDownEvent, (_, _) => Forget(), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(GotFocusEvent, (_, e) =>
        {
            if (!ReferenceEquals(e.Source, this))
                actionX = actionY = ScrollAction.FocusRequest;
        }, handledEventsToo: true);
    }

    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    // The controls it holds are in its template, which a viewer applies as it is first measured, and a hidden one never is: it is
    // applied as the viewer joins its form, so they are in the visual tree, hidden with their container, from the start.
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        ApplyTemplate();
        Presenter?.UpdateChild();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (horizontal is not null)
            horizontal.Scroll -= OnHorizontalBarScroll;
        if (vertical is not null)
            vertical.Scroll -= OnVerticalBarScroll;
        base.OnApplyTemplate(e);
        horizontal = e.NameScope.Find<ScrollBar>("PART_HorizontalScrollBar");
        vertical = e.NameScope.Find<ScrollBar>("PART_VerticalScrollBar");
        if (horizontal is not null)
            horizontal.Scroll += OnHorizontalBarScroll;
        if (vertical is not null)
            vertical.Scroll += OnVerticalBarScroll;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // The base viewer sets its own properties before this one has its component.
        if (change.Property != OffsetProperty || syncing || instance is null)
            return;
        // A layout that changes the extent places the offset the component asks for again: nobody scrolled.
        var offset = Offset;
        var asked = Asked();
        if (Math.Abs(offset.X - asked.X) < Tolerance && Math.Abs(offset.Y - asked.Y) < Tolerance)
            return;
        scrolled += change.GetNewValue<Vector>() - change.GetOldValue<Vector>();
        syncing = true;
        try
        {
            instance.Set(P.ScrollLeft, offset.X).Set(P.ScrollTop, offset.Y);
        }
        finally
        {
            syncing = false;
        }
        if (posted)
            return;
        posted = true;
        Dispatcher.UIThread.Post(RaiseScrolled);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyModifiers == KeyModifiers.None)
        {
            switch (e.Key)
            {
                case Key.Left: actionX = ScrollAction.LineUp; break;
                case Key.Right: actionX = ScrollAction.LineDown; break;
                case Key.Up: actionY = ScrollAction.LineUp; break;
                case Key.Down: actionY = ScrollAction.LineDown; break;
                case Key.PageUp: actionY = ScrollAction.PageUp; break;
                case Key.PageDown: actionY = ScrollAction.PageDown; break;
                case Key.Home: actionY = ScrollAction.Begin; break;
                case Key.End: actionY = ScrollAction.End; break;
            }
        }
        base.OnKeyDown(e);
    }

    private void OnChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        Apply(e.Property);
        if (!syncing && (ReferenceEquals(e.Property, P.ScrollLeft) || ReferenceEquals(e.Property, P.ScrollTop)))
            ScrolledByCode(ReferenceEquals(e.Property, P.ScrollLeft), (double)e.After! - (double)e.Before!);
    }

    private void ApplyAll()
    {
        foreach (var property in new PropertyDefinition[] { P.ContainerScrollBars, P.KeepScrollBarsVisible, P.ScrollWidth, P.ScrollHeight, P.Width, P.Height })
            Apply(property);
        Place(Wanted());
    }

    private void Apply(PropertyDefinition property)
    {
        if (ReferenceEquals(property, P.ContainerScrollBars) || ReferenceEquals(property, P.KeepScrollBarsVisible))
        {
            HorizontalScrollBarVisibility = VisibilityOf(ScrollBars.Horizontal);
            VerticalScrollBarVisibility = VisibilityOf(ScrollBars.Vertical);
        }
        else if (ReferenceEquals(property, P.ScrollWidth))
            area.MinWidth = Math.Max(0, instance.Get(P.ScrollWidth));
        else if (ReferenceEquals(property, P.ScrollHeight))
            area.MinHeight = Math.Max(0, instance.Get(P.ScrollHeight));
        else if (followsSize && ReferenceEquals(property, P.Width))
            Width = instance.Get(P.Width);
        else if (followsSize && ReferenceEquals(property, P.Height))
            Height = instance.Get(P.Height);
    }

    private ScrollBarVisibility VisibilityOf(ScrollBars direction) =>
        (instance.Get(P.ContainerScrollBars) & direction) == 0 ? ScrollBarVisibility.Hidden
        : (instance.Get(P.KeepScrollBarsVisible) & direction) != 0 ? ScrollBarVisibility.Visible
        : ScrollBarVisibility.Auto;

    // The offset the component asks for, and that offset within what there is to scroll.
    private Vector Wanted() => new(instance.Get(P.ScrollLeft), instance.Get(P.ScrollTop));

    private Vector Asked() => new(
        Math.Clamp(instance.Get(P.ScrollLeft), 0, Math.Max(0, Extent.Width - Viewport.Width)),
        Math.Clamp(instance.Get(P.ScrollTop), 0, Math.Max(0, Extent.Height - Viewport.Height)));

    private void Place(Vector offset)
    {
        syncing = true;
        try
        {
            Offset = offset;
        }
        finally
        {
            syncing = false;
        }
    }

    private void ScrolledByCode(bool across, double requested)
    {
        var before = Offset;
        Place(Wanted());
        var actual = Offset - before;
        if (Math.Abs(actual.X) < Tolerance && Math.Abs(actual.Y) < Tolerance)
            return;
        Raise(across ? ScrollAction.PropertyChange : ScrollAction.NoChange, across ? ScrollAction.NoChange : ScrollAction.PropertyChange,
            across ? requested : 0, across ? 0 : requested, actual.X, actual.Y);
    }

    private void RaiseScrolled()
    {
        posted = false;
        var (dx, dy) = (scrolled.X, scrolled.Y);
        scrolled = default;
        if (Math.Abs(dx) < Tolerance && Math.Abs(dy) < Tolerance)
            return;
        Raise(ActionOf(dx, actionX), ActionOf(dy, actionY), dx, dy, dx, dy);
        Forget();
    }

    private static ScrollAction ActionOf(double distance, ScrollAction? said) =>
        Math.Abs(distance) < Tolerance ? ScrollAction.NoChange
        : said ?? (distance < 0 ? ScrollAction.LineUp : ScrollAction.LineDown);

    private void Forget() => actionX = actionY = null;

    private void OnHorizontalBarScroll(object? sender, ScrollEventArgs e) => actionX = BarAction(e.ScrollEventType) ?? actionX;

    private void OnVerticalBarScroll(object? sender, ScrollEventArgs e) => actionY = BarAction(e.ScrollEventType) ?? actionY;

    private static ScrollAction? BarAction(ScrollEventType type) => type switch
    {
        ScrollEventType.SmallDecrement => ScrollAction.LineUp,
        ScrollEventType.SmallIncrement => ScrollAction.LineDown,
        ScrollEventType.LargeDecrement => ScrollAction.PageUp,
        ScrollEventType.LargeIncrement => ScrollAction.PageDown,
        ScrollEventType.ThumbTrack => ScrollAction.AbsoluteChange,
        _ => null,
    };

    private void Raise(ScrollAction actionAcross, ScrollAction actionDown, double requestDx, double requestDy, double actualDx, double actualDy) =>
        RDFormsControl.Raise(owner, Events.ContainerScroll,
            FormValue.FromLong((int)actionAcross), FormValue.FromLong((int)actionDown),
            FormValue.FromSingle((float)requestDx), FormValue.FromSingle((float)requestDy),
            FormValue.FromSingle((float)actualDx), FormValue.FromSingle((float)actualDy));
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// A ListBox's or ComboBox's TopIndex, as measured with VB6: the item in its list's top row. It is at most the item that fills the last
// row, the number of items less the rows shown. The list's viewer is scrolled to it, and the user's scroll moves it: the scroll bar's
// raises Scroll once the list has scrolled, and the wheel's raises none. Where the list moves it, the list says whether Scroll is raised.
internal sealed class ListTop
{
    private const double Tolerance = 0.5;
    private readonly Control owner;
    private readonly Func<int> count;
    private readonly Func<int> rows;
    private readonly Func<double> row;
    private ScrollViewer? viewer;
    private ScrollBar? bar;
    // Set while the top is placed in the viewer, whose offset then follows it.
    private bool placing;
    // Set by the scroll bar's scroll, until the scroll it started is done.
    private bool byBar;
    private bool moved;
    private bool posted;
    // The top as code or the last scroll left it, before a scroll still to settle.
    private int settled;

    public ListTop(Control owner, Func<int> count, Func<int> rows, Func<double> row)
    {
        this.owner = owner;
        this.count = count;
        this.rows = rows;
        this.row = row;
        // Each press, wheel turn and key starts a scroll of its own.
        owner.AddHandler(InputElement.PointerPressedEvent, (_, _) => byBar = false, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerWheelChangedEvent, (_, _) => byBar = false, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.KeyDownEvent, (_, _) => byBar = false, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    public int Value { get; private set; }

    // The rows shown, at least one.
    public int Rows => Math.Max(1, rows());

    // The last top: the item in the top row where the last item fills the last row.
    public int Last => Math.Max(0, count() - Rows);

    // Moves the top to an item, the last top at most, and raises Scroll where it moved and raise is set. Returns whether it moved.
    public bool MoveTo(int item, bool raise)
    {
        var top = Math.Clamp(item, 0, Last);
        settled = top;
        if (top == Value)
            return false;
        Value = top;
        Place();
        if (raise)
            RDFormsControl.Raise(owner, Events.Scroll);
        return true;
    }

    // Moves the top as little as shows an item: the item goes in the top row from above, and in the last row from below.
    public bool Show(int item, bool raise) => MoveTo(Showing(Value, item), raise);

    // The top that shows an item, moved as little as it takes from a top.
    private int Showing(int top, int item) =>
        item < 0 || item >= top && item < top + Rows ? top : item < top ? item : item - Rows + 1;

    // Keeps the top where it is, the last top at most, as items are added and removed.
    public void Keep() => MoveTo(Value, raise: false);

    // A key, and where typed is set a character typed, that moves the list's selection brings its item into view, as code's ListIndex
    // does, and raises Scroll after its Click where the top moved. The item is the one that takes the selection or, on a multi-select
    // list, the focus.
    public void FollowKeys(Func<int> item, bool typed)
    {
        // The list's own key handling may scroll, and further than VB6's, before any handler of this one sees the key: the key moves the
        // top from where it last settled.
        void End(object? sender, RoutedEventArgs e)
        {
            var before = settled;
            MoveTo(Showing(before, item()), raise: false);
            Place();
            if (Value != before)
                RDFormsControl.Raise(owner, Events.Scroll);
        }
        owner.AddHandler(InputElement.KeyDownEvent, End, RoutingStrategies.Bubble, handledEventsToo: true);
        if (typed)
            owner.AddHandler(InputElement.TextInputEvent, End, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    // The viewer that shows the list's rows: it is scrolled to the top from now on.
    public void Attach(ScrollViewer? shown)
    {
        if (ReferenceEquals(shown, viewer))
            return;
        if (viewer is not null)
        {
            viewer.PropertyChanged -= OnViewerChanged;
            viewer.TemplateApplied -= OnViewerTemplate;
        }
        AttachBar(null);
        viewer = shown;
        if (viewer is null)
            return;
        viewer.PropertyChanged += OnViewerChanged;
        viewer.TemplateApplied += OnViewerTemplate;
        Place();
    }

    // Scrolls the viewer to the top.
    public void Place()
    {
        if (viewer is null)
            return;
        placing = true;
        try
        {
            viewer.Offset = viewer.Offset.WithY(Value * row());
        }
        finally
        {
            placing = false;
        }
    }

    private void OnViewerTemplate(object? sender, TemplateAppliedEventArgs e) => AttachBar(e.NameScope.Find<ScrollBar>("PART_VerticalScrollBar"));

    private void AttachBar(ScrollBar? shown)
    {
        if (bar is not null)
            bar.Scroll -= OnBarScroll;
        bar = shown;
        if (bar is not null)
            bar.Scroll += OnBarScroll;
    }

    private void OnBarScroll(object? sender, ScrollEventArgs e)
    {
        byBar = true;
        Post();
    }

    private void OnViewerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // A layout that changes the extent or the rows shown scrolls the viewer to the top again: nobody scrolled.
        if (e.Property == ScrollViewer.ExtentProperty || e.Property == ScrollViewer.ViewportProperty)
            Dispatcher.UIThread.Post(Place);
        // A viewer not laid out, or hidden, shows no rows: its offset says nothing of the top.
        if (e.Property != ScrollViewer.OffsetProperty || placing || viewer is null || viewer.Viewport.Height <= 0)
            return;
        var shown = Shown(viewer);
        moved |= shown != Value;
        Value = shown;
        Post();
    }

    // The top the viewer shows: the last top where it is scrolled to its end, which a row cut short leaves short of a whole row.
    private int Shown(ScrollViewer shown)
    {
        var end = shown.Extent.Height - shown.Viewport.Height;
        var top = shown.Offset.Y >= end - Tolerance ? Last : (int)Math.Round(shown.Offset.Y / row(), MidpointRounding.AwayFromZero);
        return Math.Clamp(top, 0, Last);
    }

    private void Post()
    {
        if (posted)
            return;
        posted = true;
        Dispatcher.UIThread.Post(Settle);
    }

    private void Settle()
    {
        posted = false;
        // The user's scroll ends on a whole row: VB6's list scrolls by items.
        Place();
        if (moved && byBar)
            RDFormsControl.Raise(owner, Events.Scroll);
        moved = byBar = false;
        settled = Value;
    }
}

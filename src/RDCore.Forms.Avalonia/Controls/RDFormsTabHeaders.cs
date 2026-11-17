using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// What a MultiPage's or a TabStrip's tab shows: its caption, its tip, and whether it can be selected and is shown.
internal readonly record struct TabHeader(string Caption, string ToolTipText, bool Enabled, bool Visible);

// The tabs of a MultiPage or a TabStrip, on the side its TabOrientation names: in one row, cut off where they do not fit, or wrapped onto
// more rows (MultiRow). A tab is as wide as its caption, or TabFixedWidth, and as high as its font's line, or TabFixedHeight. As tabs, the
// selected one has the page's background and stands out from the others; as buttons, the selected one is pressed. With Style None the
// strip shows nothing and takes no room. A hidden tab takes no room either, and keeps its index.
internal sealed class RDFormsTabHeaders : Control
{
    private static readonly IBrush Edge = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.ButtonShadow));
    private static readonly IBrush Pressed = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.ButtonShadow));

    private readonly List<Border> shown = [];
    private Panel row = new StackPanel();

    public RDFormsTabHeaders()
    {
        ClipToBounds = true;
        VisualChildren.Add(row);
        LogicalChildren.Add(row);
    }

    // The tabs as last shown, every one of them, the hidden ones included.
    public IReadOnlyList<TabHeader> Tabs { get; private set; } = [];

    public int Selected { get; private set; } = -1;

    public void Show(IReadOnlyList<TabHeader> tabs, int selected, TabStyle style, TabOrientation orientation, bool multiRow, double fixedWidth,
        double fixedHeight, IBrush background)
    {
        Tabs = tabs;
        Selected = selected;
        IsVisible = style != TabStyle.None;
        var vertical = orientation is TabOrientation.Left or TabOrientation.Right;
        var direction = vertical ? Orientation.Vertical : Orientation.Horizontal;
        Panel wanted = multiRow ? new WrapPanel { Orientation = direction } : new StackPanel { Orientation = direction };
        if (wanted.GetType() != row.GetType() || (row is StackPanel s ? s.Orientation : ((WrapPanel)row).Orientation) != direction)
        {
            VisualChildren.Remove(row);
            LogicalChildren.Remove(row);
            row = wanted;
            VisualChildren.Add(row);
            LogicalChildren.Add(row);
        }

        row.Children.Clear();
        shown.Clear();
        for (var index = 0; index < tabs.Count; index++)
        {
            var tab = tabs[index];
            if (!tab.Visible)
                continue;
            var isSelected = index == selected;
            var header = new Border
            {
                Tag = index,
                Child = RDFormsCaption.Present(tab.Caption),
                Padding = new Thickness(6, 2),
                BorderBrush = Edge,
                BorderThickness = style == TabStyle.Buttons ? new Thickness(1) : OpenSide(orientation),
                Margin = style == TabStyle.Buttons ? new Thickness(1) : isSelected ? default : Away(orientation),
                Background = style == TabStyle.Buttons && isSelected ? Pressed : background,
                // Drawn disabled and left enabled: a disabled control takes no pointer, and the press on a disabled tab is still its own
                // control's, which refuses to select it.
                Opacity = tab.Enabled ? 1 : 0.5,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            if (fixedWidth > 0)
                header.Width = fixedWidth;
            if (fixedHeight > 0)
                header.Height = fixedHeight;
            ToolTip.SetTip(header, tab.ToolTipText.Length > 0 ? tab.ToolTipText : null);
            row.Children.Add(header);
            shown.Add(header);
        }
    }

    // The index of the shown tab under a point of this strip, or -1 where there is none.
    public int IndexAt(Point point)
    {
        if (!IsVisible)
            return -1;
        foreach (var header in shown)
        {
            if (header.TranslatePoint(default, this) is { } origin && new Rect(origin, header.Bounds.Size).Contains(point))
                return (int)header.Tag!;
        }

        return -1;
    }

    // The captions as the shown tabs show them, their access key markers taken out, in their order.
    public IReadOnlyList<string> Captions => [.. shown.Select(h => RDFormsCaption.AsShown(Tabs[(int)h.Tag!].Caption))];

    protected override Size MeasureOverride(Size availableSize)
    {
        row.Measure(availableSize);
        return row.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        row.Arrange(new Rect(finalSize));
        return finalSize;
    }

    // A tab is open on the side the pages are on.
    private static Thickness OpenSide(TabOrientation orientation) => orientation switch
    {
        TabOrientation.Bottom => new Thickness(1, 0, 1, 1),
        TabOrientation.Left => new Thickness(1, 1, 0, 1),
        TabOrientation.Right => new Thickness(0, 1, 1, 1),
        _ => new Thickness(1, 1, 1, 0),
    };

    // A tab not selected stands back from the pages by two pixels.
    private static Thickness Away(TabOrientation orientation) => orientation switch
    {
        TabOrientation.Bottom => new Thickness(0, 0, 0, 2),
        TabOrientation.Left => new Thickness(2, 0, 0, 0),
        TabOrientation.Right => new Thickness(0, 0, 2, 0),
        _ => new Thickness(0, 2, 0, 0),
    };
}

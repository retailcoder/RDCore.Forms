using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

// A TextBox's underline and strikethrough, which Avalonia's text presenter does not draw, as measured with VB6: each line of its text
// is underlined and struck through along its whole width, its spaces included, at the lines Print draws for the font, and an empty
// line has neither. Under the selection they are drawn in the selected text's colour. They are drawn over the presenter, in the panel
// that scrolls it.
internal sealed class TextBoxLines : Control
{
    private readonly TextPresenter presenter;
    private readonly Func<FormFont> font;

    public TextBoxLines(TextPresenter presenter, Func<FormFont> font)
    {
        this.presenter = presenter;
        this.font = font;
        IsHitTestVisible = false;
        // The presenter's text is laid out again for every change of its text, font, size or wrapping.
        presenter.LayoutUpdated += (_, _) => InvalidateVisual();
        presenter.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextPresenter.SelectionStartProperty || e.Property == TextPresenter.SelectionEndProperty
                || e.Property == TextPresenter.ShowSelectionHighlightProperty || e.Property == TextElement.ForegroundProperty
                || e.Property == TextPresenter.SelectionForegroundBrushProperty)
                InvalidateVisual();
        };
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        var print = PrintFont.Of(font());
        (int Top, int Thickness)?[] bands = [print.Underline, print.Strikethrough];
        if (bands.All(band => band is null) || presenter.Foreground is not { } ink)
            return;
        var layout = presenter.TextLayout;
        var origin = presenter.Bounds.Position - Bounds.Position;
        var (start, end) = (Math.Min(presenter.SelectionStart, presenter.SelectionEnd), Math.Max(presenter.SelectionStart, presenter.SelectionEnd));
        var selected = presenter.ShowSelectionHighlight && end > start ? layout.HitTestTextRange(start, end - start).ToList() : [];
        var selectedInk = presenter.SelectionForegroundBrush ?? ink;
        var top = 0d;
        foreach (var line in layout.TextLines)
        {
            var (left, width) = (line.Start, line.WidthIncludingTrailingWhitespace);
            var baseline = Math.Round(top + line.Baseline, MidpointRounding.AwayFromZero);
            foreach (var band in bands)
            {
                if (band is not { } stroke)
                    continue;
                var y = baseline - print.Ascent + stroke.Top;
                context.FillRectangle(ink, new Rect(origin.X + left, origin.Y + y, width, stroke.Thickness));
                // The selected part of the line, in the selected text's colour.
                foreach (var part in selected.Where(part => part.Top < top + line.Height && part.Bottom > top))
                {
                    var (from, to) = (Math.Max(part.Left, left), Math.Min(part.Right, left + width));
                    if (to > from)
                        context.FillRectangle(selectedInk, new Rect(origin.X + from, origin.Y + y, to - from, stroke.Thickness));
                }
            }
            top += line.Height;
        }
    }
}

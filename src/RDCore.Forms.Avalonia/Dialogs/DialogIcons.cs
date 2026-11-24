using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using RDCore.SDK.Runtime.Abstract.StdLib;
using Path = Avalonia.Controls.Shapes.Path;

namespace RDCore.Forms.Avalonia.Dialogs;

// The icons of a message box, drawn rather than taken from a system: a red disc with a cross, a blue disc with a question mark or an "i", and a yellow
// triangle with an exclamation mark, 32 pixels square as Windows shows them.
internal static class DialogIcons
{
    public const double Size = 32;

    private static readonly IBrush Red = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
    private static readonly IBrush Blue = new SolidColorBrush(Color.FromRgb(0x00, 0x63, 0xB1));
    private static readonly IBrush Yellow = new SolidColorBrush(Color.FromRgb(0xFF, 0xB9, 0x00));

    // The icon of the style's icon, or null for none (VBMsgBoxButtons.IconOf).
    public static Control? Of(VBMsgBoxStyle icon) => icon switch
    {
        VBMsgBoxStyle.VBCritical => Disc(Red, Cross()),
        VBMsgBoxStyle.VBQuestion => Disc(Blue, Glyph("?", Brushes.White)),
        VBMsgBoxStyle.VBExclamation => Triangle(),
        VBMsgBoxStyle.VBInformation => Disc(Blue, Glyph("i", Brushes.White)),
        _ => null,
    };

    private static Grid Disc(IBrush fill, Control mark) => Icon(new Ellipse { Fill = fill }, mark);

    private static Grid Triangle()
        => Icon(new Path { Fill = Yellow, Data = Geometry.Parse("M 16,2 L 31,29 L 1,29 Z") }, Glyph("!", Brushes.Black, top: 4));

    private static Grid Icon(Shape shape, Control mark)
    {
        var icon = new Grid { Width = Size, Height = Size };
        icon.Children.Add(shape);
        icon.Children.Add(mark);
        return icon;
    }

    private static Path Cross() => new()
    {
        Data = Geometry.Parse("M 11,11 L 21,21 M 21,11 L 11,21"),
        Stroke = Brushes.White,
        StrokeThickness = 3,
        StrokeLineCap = PenLineCap.Round,
    };

    private static TextBlock Glyph(string text, IBrush foreground, double top = 0) => new()
    {
        Text = text,
        Foreground = foreground,
        FontSize = 20,
        FontWeight = FontWeight.Bold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(0, top, 0, 0),
    };
}

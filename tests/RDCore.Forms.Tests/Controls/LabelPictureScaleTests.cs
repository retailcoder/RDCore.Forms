using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using P = RDCore.Forms.Model.Properties;
using TextAlignment = RDCore.Forms.Model.TextAlignment;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// Where a Label draws a centred picture, read from the pixels it renders, at one and at two device pixels to the pixel of the form, as a
// Retina display renders it. The cell is a game board's: 24 pixels square, transparent, no caption, its picture smaller than it. An empty
// caption still takes a line, as in MSForms: the picture and that line are centred together, and the line's height is the font's.
[TestClass]
public sealed class LabelPictureScaleTests
{
    private const uint Opaque = 0xFF0000FF;
    private const int Cell = 24;

    private sealed record Rendered(PixelRect Drawn, Rect LaidOut, Size Caption);

    // the box, in pixels of the form, of the opaque pixels the label renders at the scale given; where it lays its picture out; and the size
    // of its empty caption.
    private static Rendered Render(int pictureWidth, int pictureHeight, double scale) => UI.Run(() =>
    {
        var label = new ComponentInstance(ComponentClasses.Label, "Cell").Set(P.Caption, string.Empty).Set(P.Alignment, TextAlignment.Center)
            .Set(P.BackStyle, BackStyle.Transparent).Set(P.PicturePosition, PicturePosition.Center)
            .Set(P.Picture, Png(pictureWidth, pictureHeight, [.. Enumerable.Repeat(Opaque, pictureWidth * pictureHeight)]));
        var control = new RDFormsLabel(label);
        var (window, _) = ControlHost.Show((control, new Rect(0, 0, Cell, Cell)));
        Dispatcher.UIThread.RunJobs();
        var caption = control.GetVisualDescendants().OfType<TextBlock>().Single().Bounds.Size;
        var laidOut = control.PictureBounds!.Value;

        var size = new PixelSize((int)(Cell * scale), (int)(Cell * scale));
        using var target = new RenderTargetBitmap(size, new Vector(96 * scale, 96 * scale));
        target.Render(control);
        var stride = size.Width * 4;
        var pixels = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try
        {
            target.CopyPixels(new PixelRect(size), handle.AddrOfPinnedObject(), pixels.Length, stride);
        }
        finally
        {
            handle.Free();
        }
        window.Close();

        var (left, top, right, bottom) = (int.MaxValue, int.MaxValue, -1, -1);
        for (var y = 0; y < size.Height; y++)
        {
            for (var x = 0; x < size.Width; x++)
            {
                if (pixels[y * stride + x * 4 + 3] == 255)
                    (left, top, right, bottom) = (Math.Min(left, x), Math.Min(top, y), Math.Max(right, x), Math.Max(bottom, y));
            }
        }
        var drawn = right < 0 ? default
            : new PixelRect((int)(left / scale), (int)(top / scale), (int)((right + 1 - left) / scale), (int)((bottom + 1 - top) / scale));
        return new Rendered(drawn, laidOut, caption);
    });

    // centred with the caption's line in the cell, each offset rounded down, as MSForms' Label lays out Center (12).
    private static double Centred(double picture, double caption)
    {
        var block = Math.Max(picture, caption);
        return Math.Floor((Cell - block) / 2) + Math.Floor((block - picture) / 2);
    }

    [TestMethod]
    [DataRow(17, 21, 1d, DisplayName = "a big duck, one device pixel to the form's")]
    [DataRow(17, 21, 2d, DisplayName = "a big duck, two device pixels to the form's, as a Retina display")]
    [DataRow(11, 14, 1d, DisplayName = "a small duck, one device pixel to the form's")]
    [DataRow(11, 14, 2d, DisplayName = "a small duck, two device pixels to the form's, as a Retina display")]
    public void ACentredPicture_IsDrawnAtItsSize_WhereTheLabelLaysItOut_InItsCentre(int width, int height, double scale)
    {
        var rendered = Render(width, height, scale);

        Assert.AreEqual(new PixelRect((int)rendered.LaidOut.X, (int)rendered.LaidOut.Y, width, height), rendered.Drawn);
        Assert.AreEqual(Centred(width, rendered.Caption.Width), rendered.LaidOut.X);
        Assert.AreEqual(Centred(height, rendered.Caption.Height), rendered.LaidOut.Y);
    }

    [TestMethod]
    public void ABigDuck_IsCentredInTheCell_WhateverTheFont()
        // taller than the caption's line in any font a cell uses, and wider than it: centred on its own.
        => Assert.AreEqual(new PixelRect((Cell - 17) / 2, (Cell - 21) / 2, 17, 21), Render(17, 21, 1).Drawn);
}

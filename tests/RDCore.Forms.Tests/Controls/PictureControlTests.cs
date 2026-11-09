using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using static RDCore.Forms.Tests.Model.TestPictures;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class PictureControlTests
{
    private static readonly Color Blue = Color.FromRgb(0, 0, 255), White = Color.FromRgb(255, 255, 255),
        Red = Color.FromRgb(255, 0, 0), Green = Color.FromRgb(0, 255, 0);

    // A PNG of the size given, which Avalonia writes, its pixels the premultiplied BGRA values given row by row, transparent past them.
    internal static byte[] Png(int width, int height, params uint[] pixels)
    {
        using var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using (var buffer = bitmap.Lock())
            for (var i = 0; i < width * height; i++)
                Marshal.WriteInt32(buffer.Address, i / width * buffer.RowBytes + i % width * 4, i < pixels.Length ? (int)pixels[i] : 0);
        using var stream = new MemoryStream();
        bitmap.Save(stream);
        return stream.ToArray();
    }

    // The colour of a pixel of the window's next frame.
    internal static Color[] PixelsAt(Window window, params PixelPoint[] points)
    {
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("no frame was rendered");
        using var buffer = frame.Lock();
        return [.. points.Select(point =>
        {
            var value = Marshal.ReadInt32(buffer.Address, point.Y * buffer.RowBytes + point.X * 4);
            var (b, g, r) = ((byte)value, (byte)(value >> 8), (byte)(value >> 16));
            return buffer.Format == PixelFormats.Rgba8888 ? Color.FromRgb(b, g, r) : Color.FromRgb(r, g, b);
        })];
    }

    [TestMethod]
    public void EachDrawnFormat_DecodesToItsSizeInPixels()
    {
        var sizes = UI.Run(() =>
        {
            var png = Png(3, 2);
            return new[] { Bitmap2x2, Gif1x1, png, Icon(1, png), Icon(2, png) }.Select(bytes =>
            {
                using var bitmap = VBPicture.Decode(bytes, out var error);
                return (bitmap?.PixelSize, error);
            }).ToArray();
        });

        CollectionAssert.AreEqual(new (PixelSize?, FormError?)[]
        {
            (new PixelSize(2, 2), null), (new PixelSize(1, 1), null), (new PixelSize(3, 2), null), (new PixelSize(3, 2), null),
            (new PixelSize(3, 2), null),
        }, sizes);
    }

    [TestMethod]
    public void AMetafile_IsNotDrawn_AndIsNoError()
    {
        var (bitmap, error) = UI.Run(() => (VBPicture.Decode(Hex("D7CDC69A 0000 00000000 64006400 6009 00000000 0000"), out var error), error));

        Assert.IsNull(bitmap);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void APictureWhoseHeaderPasses_ButWhoseBodyTheDecoderCannotRead_Is481()
    {
        var (bitmap, error) = UI.Run(() => (VBPicture.Decode([.. JpegHeader(2, 2), 0xFF, 0xD9], out var error), error));

        Assert.IsNull(bitmap);
        Assert.AreEqual(FormError.InvalidPicture, error);
    }

    [TestMethod]
    public void EveryPictureCutShortOrChanged_DecodesWithinTheBounds_OrIs481_AndNeverThrows()
    {
        var decoded = UI.Run(() =>
        {
            var png = Png(3, 2);
            var count = 0;
            foreach (var (picture, seed) in new[] { Bitmap2x2, Gif1x1, png, Icon(1, png) }.Select((p, i) => (p, i)))
            {
                foreach (var bytes in Malformed(picture, seed, 150))
                {
                    using var bitmap = VBPicture.Decode(bytes, out var error);
                    if (bitmap is null)
                        Assert.IsTrue(error is null ? bytes.Length == 0 || FormPictures.Inspect(bytes, out var info) is null && !info.IsDrawn
                            : error == FormError.InvalidPicture);
                    else
                        Assert.IsTrue(bitmap.PixelSize.Width is > 0 and <= FormPictures.MaxSide && bitmap.PixelSize.Height is > 0 and <= FormPictures.MaxSide);
                    count++;
                }
            }
            return count;
        });

        Assert.IsGreaterThan(600, decoded);
    }

    [TestMethod]
    public void AnImagesPicture_IsDrawnAtItsSizeFromItsCorner_OrStretchedOverIt()
    {
        var (atItsSize, stretched) = UI.Run(() =>
        {
            var image = new VBImage(new ComponentInstance(VBClasses.Image, "Image1").Set(P.Picture, Bitmap2x2));
            var (window, _) = ControlHost.Show((image, new Rect(10, 5, 40, 40)));
            var atItsSize = PixelsAt(window, new(10, 5), new(11, 5), new(10, 6), new(11, 6));
            VBControl.GetInstance(image)!.Set(P.Stretch, true);
            var stretched = PixelsAt(window, new(20, 15), new(40, 15), new(20, 35), new(40, 35));
            window.Close();
            return (atItsSize, stretched);
        });

        CollectionAssert.AreEqual(new[] { Blue, White, Red, Green }, atItsSize);
        CollectionAssert.AreEqual(new[] { Blue, White, Red, Green }, stretched);
    }

    [TestMethod]
    public void APictureBoxsPicture_IsDrawnFromItsClientOrigin_UnderTheControlsItHolds()
    {
        var (size, pixels) = UI.Run(() =>
        {
            var picture = new VBPictureBox(new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Picture, Bitmap2x2)
                .Set(P.BorderStyle, BorderStyle.None));
            var (window, _) = ControlHost.Show((picture, new Rect(10, 5, 40, 40)));
            var pixels = PixelsAt(window, new(10, 5), new(11, 6));
            window.Close();
            return (picture.PictureSize, pixels);
        });

        Assert.AreEqual(new PixelSize(2, 2), size);
        CollectionAssert.AreEqual(new[] { Blue, Green }, pixels);
    }

    [TestMethod]
    public void APngsTransparentPixels_ShowWhatIsBehindThem_AndItsOpaqueOnesAreDrawn()
    {
        var pixels = UI.Run(() =>
        {
            // Opaque red, then transparent, then red at half its alpha.
            var png = Png(3, 1, 0xFFFF0000, 0x00000000, 0x80800000);
            var picture = new VBPictureBox(new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Picture, png)
                .Set(P.BorderStyle, BorderStyle.None).Set(P.BackColor, FormColor.FromRgb(0, 0, 255)));
            var (window, _) = ControlHost.Show((picture, new Rect(10, 5, 40, 40)));
            var pixels = PixelsAt(window, new(10, 5), new(11, 5), new(12, 5));
            window.Close();
            return pixels;
        });

        Assert.AreEqual(Red, pixels[0]);
        Assert.AreEqual(Blue, pixels[1]);
        // Half red over the blue back colour.
        Assert.IsTrue(pixels[2] is { R: >= 120 and <= 136, G: 0, B: >= 119 and <= 135 }, pixels[2].ToString());
    }

    [TestMethod]
    public void AMalformedPicture_IsNotDrawn_AndItsControlSays481_AndTheFormStillShows()
    {
        var (imageError, imageSize, boxError) = UI.Run(() =>
        {
            var image = new VBImage(new ComponentInstance(VBClasses.Image, "Image1").Set(P.Picture, Hex("424D 00000000 00000000 FF000000")));
            var box = new VBPictureBox(new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.Picture, Hex("89504E47")));
            var (window, _) = ControlHost.Show((image, new Rect(10, 5, 40, 40)), (box, new Rect(60, 5, 40, 40)));
            window.Close();
            return (image.PictureError, image.PictureSize, box.PictureError);
        });

        Assert.AreEqual(FormError.InvalidPicture, imageError);
        Assert.IsNull(imageSize);
        Assert.AreEqual(FormError.InvalidPicture, boxError);
    }
}

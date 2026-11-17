using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using static RDCore.Forms.Tests.Model.TestPictures;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

/// <summary>
/// The pixels a picture is drawn with: those the SDK decodes, alpha included, of the picture <c>LoadPicture</c> makes of the same bytes.
/// </summary>
[TestClass]
public sealed class PictureDecodeTests
{
    // each pixel of a decoded picture as its premultiplied bytes, blue, green, red and alpha, in hex.
    private static string[] Premultiplied(byte[] picture) => UI.Run(() =>
    {
        using var bitmap = RDFormsPicture.Decode(picture, out var error);
        Assert.IsNull(error);
        using var copy = new WriteableBitmap(bitmap!.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using var buffer = copy.Lock();
        bitmap.CopyPixels(buffer);
        var row = new byte[buffer.Size.Width * 4];
        var pixels = new List<string>();
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, row.Length);
            for (var x = 0; x < row.Length; x += 4)
                pixels.Add(Convert.ToHexString(row, x, 4));
        }
        return pixels.ToArray();
    });

    // an icon's DIB image of 32 bits per pixel, one row: each pixel as its blue, green, red and alpha bytes, and no pixel masked.
    private static byte[] Dib32(params uint[] bgra)
    {
        var mask = (bgra.Length + 31) / 32 * 4;
        var dib = new byte[40 + bgra.Length * 4 + mask];
        BinaryPrimitives.WriteInt32LittleEndian(dib, 40);
        BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(4), bgra.Length);
        BinaryPrimitives.WriteInt32LittleEndian(dib.AsSpan(8), 2);
        BinaryPrimitives.WriteInt16LittleEndian(dib.AsSpan(12), 1);
        BinaryPrimitives.WriteInt16LittleEndian(dib.AsSpan(14), 32);
        for (var i = 0; i < bgra.Length; i++)
            BinaryPrimitives.WriteUInt32BigEndian(dib.AsSpan(40 + i * 4), bgra[i]);
        return dib;
    }

    // red at alpha 128, and a clear pixel: drawn premultiplied, as Avalonia draws a picture.
    [TestMethod]
    public void APngsAlpha_IsDrawn()
        => CollectionAssert.AreEqual(new[] { "00008080", "00000000" }, Premultiplied(UI.Run(() => Png(2, 1, 0x80800000, 0))));

    // red at alpha 128 and blue clear: a 32-bit image whose alpha is not all 0 is drawn with that alpha, whatever its mask.
    [TestMethod]
    [DataRow((short)1, DisplayName = "an icon")]
    [DataRow((short)2, DisplayName = "a cursor")]
    public void A32BitIconImagesAlpha_IsDrawn(short type)
        => CollectionAssert.AreEqual(new[] { "00008080", "00000000" }, Premultiplied(Icon(type, Dib32(0x0000FF80, 0xFF000000))));

    // the image of the system's icon size, 32 by 32, where the icon has one; its first image where it has none: the image LoadPicture loads when
    // asked for no size, as a designer's picture loads.
    [TestMethod]
    [DataRow(16, 32, 32, DisplayName = "16 then 32: the 32")]
    [DataRow(32, 16, 32, DisplayName = "32 then 16: the 32")]
    [DataRow(16, 48, 16, DisplayName = "16 then 48: the first")]
    public void AnIconOfSeveralImages_DrawsTheImageLoadPictureLoads(int first, int second, int drawn)
    {
        var size = UI.Run(() =>
        {
            using var bitmap = RDFormsPicture.Decode(Icon(1, Png(first, first), Png(second, second)), out _);
            return bitmap?.PixelSize;
        });

        Assert.AreEqual(new PixelSize(drawn, drawn), size);
    }

    // a bitmap of BI_JPEG pixels: LoadPicture loads it, but the SDK does not decode its pixels, so it is not drawn.
    [TestMethod]
    public void APictureWhosePixelsTheSdkDoesNotDecode_Is481()
    {
        var bitmap = Hex("424D 3A000000 00000000 36000000", "28000000 01000000 01000000 0100 1800 04000000 04000000 00000000 00000000 00000000 00000000",
            "00000000");

        var (decoded, error) = UI.Run(() => (RDFormsPicture.Decode(bitmap, out var error), error));

        Assert.IsNull(decoded);
        Assert.AreEqual(FormError.InvalidPicture, error);
    }
}

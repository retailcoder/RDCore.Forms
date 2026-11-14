using RDCore.Forms.Model;
using RDCore.SDK.Workspace.Pictures;
using static RDCore.Forms.Tests.Model.TestPictures;

namespace RDCore.Forms.Tests.Model;

// FormPictures.Inspect gives what the SDK's PictureHeader reads, and error 481 for what it refuses. The header's own rules are the SDK's
// and tested there.
[TestClass]
public sealed class FormPicturesTests
{
    private static void Refused(byte[]? bytes)
    {
        Assert.AreEqual(FormError.InvalidPicture, FormPictures.Inspect(bytes, out var info));
        Assert.AreEqual(default, info);
    }

    [TestMethod]
    public void AnAcceptedPicture_GivesWhatItsHeaderSays_AndNoError()
    {
        Assert.IsNull(FormPictures.Inspect(PngHeader(3, 2), out var png));
        Assert.IsNull(FormPictures.Inspect(Bitmap2x2, out var bitmap));
        PictureHeader.TryRead(PngHeader(3, 2), out var pngHeader);
        PictureHeader.TryRead(Bitmap2x2, out var bitmapHeader);

        Assert.AreEqual(pngHeader, png);
        Assert.AreEqual(bitmapHeader, bitmap);
        Assert.AreEqual((PictureFormat.Png, 3, 2), (png.Format, png.Width, png.Height));
        Assert.AreEqual((PictureFormat.Bitmap, 2, 2), (bitmap.Format, bitmap.Width, bitmap.Height));
    }

    [TestMethod]
    public void NoPicture_IsRefusedAs481() => Refused(null);

    [TestMethod]
    [DataRow("", DisplayName = "no bytes")]
    [DataRow("424D", DisplayName = "a bitmap's magic alone")]
    [DataRow("4D5A9000", DisplayName = "a program")]
    public void APictureTheHeaderReaderRefuses_IsRefusedAs481(string hex) => Refused(Hex(hex));

    [TestMethod]
    public void APictureOfASizePastTheBounds_IsRefusedAs481() => Refused(PngHeader(PictureHeader.MaxSide + 1, 1));
}

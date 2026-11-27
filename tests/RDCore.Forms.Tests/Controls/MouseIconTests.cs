using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Libraries;
using RDCore.Forms.Model;
using static RDCore.Forms.Tests.Model.TestPictures;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A MouseIcon, as measured with VB6 at 96 DPI, native and p-code agreeing: MousePointer 99 (Custom) shows a pointer 32 pixels square made of
// it, of the picture's 32x32 image or of its only image scaled to 32. An icon's hot spot is the centre, a cursor's its own, scaled with its
// image. With no MouseIcon the pointer is the arrow, and a bitmap is refused with 380.
[TestClass]
public sealed class MouseIconTests
{
    private const uint Red = 0xFFFF0000, Blue = 0xFF0000FF;

    // An icon (1) or a cursor (2) of opaque PNG images of one colour each, given as their side, colour and hot spot: a cursor's directory
    // entry holds the hot spot where an icon's holds its planes and bit count. Avalonia writes the PNGs, on the UI thread.
    private static byte[] File(short type, params (int Side, uint Colour, int X, int Y)[] images) => UI.Run(() =>
    {
        var bytes = Icon(type, [.. images.Select(i => PictureControlTests.Png(i.Side, i.Side, [.. Enumerable.Repeat(i.Colour, i.Side * i.Side)]))]);
        for (var i = 0; type == 2 && i < images.Length; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6 + 16 * i + 4), (ushort)images[i].X);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(6 + 16 * i + 6), (ushort)images[i].Y);
        }
        return bytes;
    });

    private static byte[] Picture(string name) => name switch
    {
        "a 32x32 icon" => File(1, (32, Blue, 0, 0)),
        "a 16x16 icon" => File(1, (16, Blue, 0, 0)),
        "a 48x48 icon" => File(1, (48, Blue, 0, 0)),
        "an icon of a 16x16 image then a 32x32 one" => File(1, (16, Red, 0, 0), (32, Blue, 0, 0)),
        "a 32x32 cursor" => File(2, (32, Blue, 5, 9)),
        "a 16x16 cursor" => File(2, (16, Blue, 3, 4)),
        "a cursor of a 16x16 image then a 32x32 one" => File(2, (16, Red, 3, 4), (32, Blue, 10, 12)),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    // The pointer's size, its hot spot, and its top left pixel as its premultiplied ARGB value.
    private static (int Width, int Height, int X, int Y, uint Corner)? Pointer(byte[]? icon) => UI.Run(() =>
    {
        using var image = RDFormsPicture.DecodePointer(icon, out var hotSpot);
        if (image is null)
            return ((int, int, int, int, uint)?)null;
        using var copy = new WriteableBitmap(image.PixelSize, new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Premul);
        using var buffer = copy.Lock();
        image.CopyPixels(buffer);
        return (image.PixelSize.Width, image.PixelSize.Height, hotSpot.X, hotSpot.Y, (uint)Marshal.ReadInt32(buffer.Address));
    });

    [TestMethod]
    [DataRow("a 32x32 icon", 16, 16)]
    [DataRow("a 16x16 icon", 16, 16)]
    [DataRow("a 48x48 icon", 16, 16)]
    [DataRow("an icon of a 16x16 image then a 32x32 one", 16, 16)]
    [DataRow("a 32x32 cursor", 5, 9)]
    [DataRow("a 16x16 cursor", 6, 8)]
    [DataRow("a cursor of a 16x16 image then a 32x32 one", 10, 12)]
    public void TheCustomPointer_Is32PixelsSquare_OfThe32x32Image_WithAnIconsHotSpotAtTheCentre_AndACursorsScaledWithIt(string picture, int x, int y)
    {
        Assert.AreEqual((32, 32, x, y, Blue), Pointer(Picture(picture)));
    }

    [TestMethod]
    public void NoMouseIcon_AndABitmap_MakeNoPointer()
    {
        Assert.IsNull(Pointer(null));
        Assert.IsNull(Pointer(Bitmap2x2));
    }

    [TestMethod]
    public void AMouseIcon_IsAnIconOrACursorOrNone_AndABitmapIsRefused()
    {
        var button = new ComponentInstance(ComponentClasses.CommandButton, "Command1");
        var cursor = File(2, (32, Blue, 5, 9));

        var accepted = new[] { button.TrySet(P.MouseIcon, File(1, (32, Blue, 0, 0))), button.TrySet(P.MouseIcon, null), button.TrySet(P.MouseIcon, cursor) };
        var refused = new[] { button.TrySet(P.MouseIcon, Bitmap2x2), button.TrySet(P.MouseIcon, PngHeader(2, 2)) };

        CollectionAssert.AreEqual(new[] { true, true, true }, accepted);
        CollectionAssert.AreEqual(new[] { false, false }, refused);
        Assert.AreSame(cursor, button.Get(P.MouseIcon));
    }

    // Every class of VB6.OLB the model has, with a MousePointer, has a MouseIcon where VB6.OLB declares one: a Shape and a Line have neither.
    [TestMethod]
    public void AClassOfVB6_HasAMouseIcon_WhereVB6OlbDeclaresOne()
    {
        Assert.IsTrue(RDFormsLibraries.Instance.TryGet("vb", out var vb));
        var classes = vb.Classes.Select(declared => (declared, model: ComponentClasses.All.SingleOrDefault(c => c.ClassName == declared.Name)))
            .Where(pair => pair.model is not null && pair.model.Has(P.MousePointer))
            .ToArray();

        foreach (var (declared, model) in classes)
            Assert.AreEqual(declared.Members.Any(m => m.Name == "MouseIcon"), model!.Has(P.MouseIcon), declared.Name);
        CollectionAssert.IsSubsetOf(new[] { "Form", "CommandButton", "TextBox", "HScrollBar", "Image", "Data" },
            classes.Select(pair => pair.declared.Name).ToArray());
    }

    // A control's pointer: none of its own for Default, the arrow for Custom with no MouseIcon or for Arrow, and a pointer of its own
    // for Custom with one.
    [TestMethod]
    public void AControlsPointer_IsMadeOfItsMouseIcon_OnlyWhereMousePointerIsCustom()
    {
        var cursor = File(2, (32, Blue, 5, 9));
        var shown = UI.Run(() =>
        {
            var instance = new ComponentInstance(ComponentClasses.CommandButton, "Command1");
            var button = new RDFormsCommandButton(instance);
            var (window, _) = ControlHost.Show(button);
            var pointers = new List<string?> { button.Cursor?.ToString() };
            instance.Set(P.MousePointer, MousePointer.Custom);
            pointers.Add(button.Cursor?.ToString());
            instance.Set(P.MouseIcon, cursor);
            pointers.Add(button.Cursor?.ToString());
            instance.Set(P.MousePointer, MousePointer.Arrow);
            pointers.Add(button.Cursor?.ToString());
            instance.Set(P.MousePointer, MousePointer.Custom);
            pointers.Add(button.Cursor?.ToString());
            instance.Set(P.MouseIcon, null);
            pointers.Add(button.Cursor?.ToString());
            window.Close();
            return pointers.ToArray();
        });

        var arrow = UI.Run(() => new Cursor(StandardCursorType.Arrow).ToString());
        CollectionAssert.AreEqual(new[] { null, arrow, shown[2], arrow, shown[2], arrow }, shown, string.Join(", ", shown));
        Assert.IsNotNull(shown[2]);
        Assert.AreNotEqual(arrow, shown[2]);
    }
}

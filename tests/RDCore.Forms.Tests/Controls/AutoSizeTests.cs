using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// An Image without Stretch and a PictureBox with AutoSize take their picture's size, as measured with VB6, native and p-code
// agreeing: inside their border, 2 pixels a side for a sunken one and 1 for a flat one; an Image's Left and Top go to whole pixels;
// a cleared picture, or a size set by code, is kept.
[TestClass]
public sealed class AutoSizeTests
{
    // 100 twips, where VB6's Left and Top start in the probe: not a whole number of pixels.
    private const double At = 100 / 15.0;

    // Made on the UI thread, as they are first used.
    private static readonly Lazy<byte[]> wide = new(() => PictureControlTests.Png(24, 20, [.. Enumerable.Repeat(0xFFFF0000u, 24 * 20)]));
    private static readonly Lazy<byte[]> other = new(() => PictureControlTests.Png(30, 10, [.. Enumerable.Repeat(0xFF0000FFu, 30 * 10)]));

    private static byte[] Wide => wide.Value;

    private static byte[] Other => other.Value;

    private static (double Left, double Top, double Width, double Height) Extent(ComponentInstance component) =>
        (component.Get(P.Left), component.Get(P.Top), component.Get(P.Width), component.Get(P.Height));

    // Shows a control at At, At, 100x60 pixels, runs the changes on its component, and gives its extent after each.
    private static List<(double Left, double Top, double Width, double Height)> After(Func<ComponentInstance, Control> make,
        ComponentInstance component, params Action<ComponentInstance>[] changes) => UI.Run(() =>
    {
        var (window, _) = ControlHost.Show((make(component), new Rect(At, At, 100, 60)));
        var extents = new List<(double Left, double Top, double Width, double Height)>();
        foreach (var change in changes)
        {
            change(component);
            Dispatcher.UIThread.RunJobs();
            extents.Add(Extent(component));
        }
        window.Close();
        return extents;
    });

    private static (double, double, double, double) Is(double left, double top, double width, double height) => (left, top, width, height);

    private static ComponentInstance Image() => new(VBClasses.Image, "Image1");

    private static ComponentInstance PictureBox() => new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.AutoSize, true);

    private static Control MakeImage(ComponentInstance component) => new VBImage(component);

    private static Control MakePictureBox(ComponentInstance component) => new VBPictureBox(component);

    [TestMethod]
    public void AnImageWithoutStretch_TakesItsPicturesSize_AndItsLeftAndTopGoToWholePixels()
    {
        var extents = After(MakeImage, Image(), c => c.Set(P.Picture, Wide));

        Assert.AreEqual(Is(7, 7, 24, 20), extents.Single());
    }

    [TestMethod]
    public void AnImageWithItsBorder_TakesItsPicturesSize_TwoPixelsMoreOnEachSide()
    {
        var extents = After(MakeImage, Image().Set(P.BorderStyle, BorderStyle.FixedSingle), c => c.Set(P.Picture, Wide));

        Assert.AreEqual(Is(7, 7, 28, 24), extents.Single());
    }

    [TestMethod]
    public void AnImageWithoutStretch_GrowsToItsPicture()
    {
        var extent = UI.Run(() =>
        {
            var component = Image();
            var (window, _) = ControlHost.Show((MakeImage(component), new Rect(At, At, 5, 5)));
            component.Set(P.Picture, Wide);
            var extent = Extent(component);
            window.Close();
            return extent;
        });

        Assert.AreEqual(Is(7, 7, 24, 20), extent);
    }

    [TestMethod]
    public void AnImageWithStretch_KeepsItsSize()
    {
        var extents = After(MakeImage, Image().Set(P.Stretch, true), c => c.Set(P.Picture, Wide));

        Assert.AreEqual(Is(At, At, 100, 60), extents.Single());
    }

    [TestMethod]
    public void AnImagesStretchCleared_TakesItsPicturesSize_AndSetAgain_KeepsIt()
    {
        var extents = After(MakeImage, Image().Set(P.Stretch, true),
            c => c.Set(P.Picture, Wide), c => c.Set(P.Stretch, false), c => c.Set(P.Stretch, true));

        CollectionAssert.AreEqual(new[] { Is(At, At, 100, 60), Is(7, 7, 24, 20), Is(7, 7, 24, 20) }, extents);
    }

    [TestMethod]
    public void AnImagesPictureCleared_KeepsItsSize()
    {
        var extents = After(MakeImage, Image(), c => c.Set(P.Picture, Wide), c => c.Set(P.Picture, (byte[]?)null));

        CollectionAssert.AreEqual(new[] { Is(7, 7, 24, 20), Is(7, 7, 24, 20) }, extents);
    }

    [TestMethod]
    public void AnImagesWidthSetByCode_IsKeptWithTheSamePicture_AndAnotherPictureTakesItsSize()
    {
        var extents = After(MakeImage, Image(),
            c => c.Set(P.Picture, Wide), c => c.Set(P.Width, 50d), c => c.Set(P.Picture, Wide), c => c.Set(P.Picture, Other));

        CollectionAssert.AreEqual(new[] { Is(7, 7, 24, 20), Is(7, 7, 50, 20), Is(7, 7, 50, 20), Is(7, 7, 30, 10) }, extents);
    }

    [TestMethod]
    public void AnImageMadeWithItsPicture_TakesItsSize_WhateverSizeItsFormHolds()
    {
        var extent = UI.Run(() =>
        {
            var component = Image().Set(P.Left, At).Set(P.Top, At).Set(P.Width, 100d).Set(P.Height, 60d).Set(P.Picture, Wide);
            _ = new VBImage(component);
            return Extent(component);
        });

        Assert.AreEqual(Is(7, 7, 24, 20), extent);
    }

    [TestMethod]
    [DataRow(BorderStyle.FixedSingle, Appearance.ThreeD, 28d, 24d)]
    [DataRow(BorderStyle.FixedSingle, Appearance.Flat, 26d, 22d)]
    [DataRow(BorderStyle.None, Appearance.ThreeD, 24d, 20d)]
    public void APictureBoxWithAutoSize_TakesItsPicturesSize_InsideItsBorder_AndKeepsItsLeftAndTop(BorderStyle border,
        Appearance appearance, double width, double height)
    {
        var extents = After(MakePictureBox, PictureBox().Set(P.BorderStyle, border).Set(P.Appearance, appearance),
            c => c.Set(P.Picture, Wide));

        Assert.AreEqual(Is(At, At, width, height), extents.Single());
    }

    [TestMethod]
    public void APictureBoxWithoutAutoSize_KeepsItsSize_UntilAutoSizeIsSet_AndClearingItKeepsIt()
    {
        var extents = After(MakePictureBox, PictureBox().Set(P.AutoSize, false),
            c => c.Set(P.Picture, Wide), c => c.Set(P.AutoSize, true), c => c.Set(P.AutoSize, false));

        CollectionAssert.AreEqual(new[] { Is(At, At, 100, 60), Is(At, At, 28, 24), Is(At, At, 28, 24) }, extents);
    }

    [TestMethod]
    public void APictureBoxsPictureCleared_KeepsItsSize_AndAWidthSetByCodeIsKept()
    {
        var extents = After(MakePictureBox, PictureBox(),
            c => c.Set(P.Picture, Wide), c => c.Set(P.Picture, (byte[]?)null), c => c.Set(P.Width, 200d));

        CollectionAssert.AreEqual(new[] { Is(At, At, 28, 24), Is(At, At, 28, 24), Is(At, At, 200, 24) }, extents);
    }

    [TestMethod]
    public void APictureBoxMadeWithItsPicture_TakesItsSize_WhateverSizeItsFormHolds()
    {
        var extent = UI.Run(() =>
        {
            var component = PictureBox().Set(P.Left, At).Set(P.Top, At).Set(P.Width, 100d).Set(P.Height, 60d).Set(P.Picture, Wide);
            _ = new VBPictureBox(component);
            return Extent(component);
        });

        Assert.AreEqual(Is(At, At, 28, 24), extent);
    }
}

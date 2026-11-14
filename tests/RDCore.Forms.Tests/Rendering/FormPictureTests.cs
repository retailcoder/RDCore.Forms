using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Tests.Controls;
using RDCore.Forms.Drawing;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using static RDCore.Forms.Tests.Model.TestPictures;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// A form's Picture, as measured with VB6, native and p-code agreeing.
[TestClass]
public sealed class FormPictureTests
{
    private static readonly Color Blue = Color.FromRgb(0, 0, 255), White = Color.FromRgb(255, 255, 255),
        Red = Color.FromRgb(255, 0, 0), Green = Color.FromRgb(0, 255, 0);

    private static ComponentInstance Root() =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 120d).Set(P.Height, 80d).Set(P.ScaleMode, ScaleMode.Pixel)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255));

    private static VBFormWindow Show(ComponentInstance root)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // The colours of the form's client area at points of it, in pixels.
    private static Color[] Client(VBFormWindow window, params (int X, int Y)[] points)
    {
        Dispatcher.UIThread.RunJobs();
        var origin = window.View.TranslatePoint(default, window)!.Value;
        return PixelsAt(window, [.. points.Select(p => new PixelPoint((int)origin.X + p.X, (int)origin.Y + p.Y))]);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AFormsPicture_IsDrawnFromItsClientOrigin_AtItsSize_UnderItsDrawing_AndClsKeepsIt(bool autoRedraw)
    {
        var (picture, drawn, cleared) = UI.Run(() =>
        {
            var window = Show(Root().Set(P.AutoRedraw, autoRedraw).Set(P.Picture, Bitmap2x2));
            var picture = Client(window, (0, 0), (1, 0), (0, 1), (1, 1), (2, 0), (0, 2));
            window.View.Drawing.PSet(new GraphicsPoint(1, 1), FormColor.FromRgb(255, 0, 0));
            var drawn = Client(window, (1, 1), (0, 0));
            window.View.Drawing.Cls();
            var cleared = Client(window, (1, 1));
            window.Close();
            return (picture, drawn, cleared[0]);
        });

        CollectionAssert.AreEqual(new[] { Blue, White, Red, Green, White, White }, picture);
        CollectionAssert.AreEqual(new[] { Red, Blue }, drawn);
        Assert.AreEqual(Green, cleared);
    }

    [TestMethod]
    public void APictureSetFromCode_IsDrawn_AndNoneClearsIt()
    {
        var (before, set, cleared) = UI.Run(() =>
        {
            var root = Root();
            var window = Show(root);
            var before = Client(window, (0, 0));
            root.Set(P.Picture, Bitmap2x2);
            var set = Client(window, (0, 0));
            root.Set(P.Picture, null);
            var cleared = Client(window, (0, 0));
            window.Close();
            return (before[0], set[0], cleared[0]);
        });

        Assert.AreEqual(White, before);
        Assert.AreEqual(Blue, set);
        Assert.AreEqual(White, cleared);
    }

    [TestMethod]
    public void TheWindowShowsTheRootsIcon_MadeOnceForEachValue_AndNoneWithoutOne()
    {
        var (shown, kept, remade, none, malformed) = UI.Run(() =>
        {
            var icon = Icon(1, Png(2, 2));
            var root = Root().Set(P.Icon, icon);
            var window = Show(root);
            var shown = window.Icon;
            root.Set(P.Caption, "Another caption");
            var kept = ReferenceEquals(shown, window.Icon);
            root.Set(P.Icon, Icon(2, Png(2, 2)));
            var remade = window.Icon is not null && !ReferenceEquals(shown, window.Icon);
            root.Set(P.Icon, null);
            var none = window.Icon;
            root.Set(P.Icon, Hex("00000100 0100"));
            var malformed = window.Icon;
            window.Close();
            return (shown, kept, remade, none, malformed);
        });

        Assert.IsNotNull(shown);
        Assert.IsTrue(kept);
        Assert.IsTrue(remade);
        Assert.IsNull(none);
        Assert.IsNull(malformed);
    }

    [TestMethod]
    public void APressOnThePicture_IsTheRoots()
    {
        var raised = UI.Run(() =>
        {
            var window = new VBFormWindow(new FormDefinition(Root().Set(P.Picture, Bitmap2x2)));
            var sink = new RecordingSink();
            VBControl.SetEventSink(window, sink);
            window.Closing += (_, _) => VBControl.SetEventSink(window, null);
            window.Show();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            sink.Raised.Clear();
            var origin = window.View.TranslatePoint(default, window)!.Value;
            window.MouseDown(origin + new Point(1, 1), MouseButton.Left);
            window.MouseUp(origin + new Point(1, 1), MouseButton.Left);
            var result = sink.WithoutMoves.ToList();
            window.Close();
            return result;
        });

        CollectionAssert.Contains(raised, "Form1.Click");
    }

    [TestMethod]
    public void AMalformedPicture_IsNotDrawn_AndTheViewSays481_AndTheFormStillShows()
    {
        var (error, colour, fine) = UI.Run(() =>
        {
            var root = Root().Set(P.Picture, Hex("424D 00000000 00000000 FF000000"));
            var window = Show(root);
            var result = (window.View.PictureError, Client(window, (0, 0))[0]);
            root.Set(P.Picture, Bitmap2x2);
            var fine = window.View.PictureError;
            window.Close();
            return (result.PictureError, result.Item2, fine);
        });

        Assert.AreEqual(FormError.InvalidPicture, error);
        Assert.AreEqual(White, colour);
        Assert.IsNull(fine);
    }
}

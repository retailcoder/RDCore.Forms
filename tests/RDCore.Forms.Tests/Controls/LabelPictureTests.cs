using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using static RDCore.Forms.Tests.Model.TestPictures;
using P = RDCore.Forms.Model.Properties;
using TextAlignment = RDCore.Forms.Model.TextAlignment;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A Label's Picture and PicturePosition, an RD-VBA addition laid out as MSForms' Label lays it out (measured in Excel), and a GIF's
// transparent colour over a form's Picture, as VB6 and MSForms draw it.
[TestClass]
public sealed class LabelPictureTests
{
    private static readonly Color Blue = Color.FromRgb(0, 0, 255), Red = Color.FromRgb(255, 0, 0), Green = Color.FromRgb(0, 255, 0),
        Yellow = Color.FromRgb(255, 255, 0), White = Color.FromRgb(255, 255, 255);

    // A 2x2 GIF: white (index 0, transparent) and blue on top, green and blue below. Its LZW data is a clear code before every two
    // pixels.
    private static readonly byte[] ClearGif = Hex(
        "474946383961 0200 0200 81 00 00 FFFFFF 0000FF 00FF00 000000 21F904 01 0000 00 00 2C 0000 0000 0200 0200 00 02 03 44A814 00 3B");

    // A 24-bit bitmap of one colour, its lower-left pixel of another.
    private static byte[] Bitmap(int width, int height, Color fill, Color lowerLeft)
    {
        var stride = (width * 3 + 3) / 4 * 4;
        var bytes = new byte[54 + stride * height];
        using var writer = new BinaryWriter(new MemoryStream(bytes));
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(bytes.Length);
        writer.Write(0);
        writer.Write(54);
        writer.Write(40);
        writer.Write(width);
        writer.Write(height);
        writer.Write((short)1);
        writer.Write((short)24);
        writer.Write(0);
        writer.Write(stride * height);
        writer.Write(2835);
        writer.Write(2835);
        writer.Write(0L);
        for (var y = 0; y < height; y++)
        {
            writer.Seek(54 + y * stride, SeekOrigin.Begin);
            for (var x = 0; x < width; x++)
            {
                var colour = x == 0 && y == 0 ? lowerLeft : fill;
                writer.Write(colour.B);
                writer.Write(colour.G);
                writer.Write(colour.R);
            }
        }
        return bytes;
    }

    private static ComponentInstance Label(double width, double height, byte[]? picture, string caption = "")
    {
        var label = new ComponentInstance(ComponentClasses.Label, "Label1").Set(P.Width, width).Set(P.Height, height).Set(P.Caption, caption)
            .Set(P.BackColor, FormColor.FromRgb(255, 255, 255));
        return picture is null ? label : label.Set(P.Picture, picture);
    }

    private static (Rect Picture, Rect Text) Laid(ComponentInstance label)
    {
        var control = new RDFormsLabel(label);
        var (window, _) = ControlHost.Show((control, new Rect(0, 0, label.Get(P.Width), label.Get(P.Height))));
        Dispatcher.UIThread.RunJobs();
        var text = control.GetVisualDescendants().OfType<TextBlock>().Single();
        var result = (control.PictureBounds!.Value, text.Bounds);
        window.Close();
        return result;
    }

    [TestMethod]
    public void APictureBesideTheCaption_IsOnePixelFromIt_TheBlockCentred_AndTheSecondWordAlignsThem()
    {
        var laid = UI.Run(() => new[] { PicturePosition.LeftTop, PicturePosition.LeftCenter, PicturePosition.LeftBottom, PicturePosition.RightTop }
            .Select(position => Laid(Label(200, 93, Bitmap(16, 40, Red, Green), "Ab").Set(P.PicturePosition, position))).ToArray());

        var (picture, text) = laid[0];
        Assert.AreEqual(new Size(16, 40), picture.Size);
        Assert.AreEqual(picture.Right + 1, text.X);
        Assert.AreEqual(Math.Floor((200 - (16 + 1 + text.Width)) / 2), picture.X);
        Assert.AreEqual(Math.Floor((93 - 40) / 2d), picture.Y);
        CollectionAssert.AreEqual(new[] { 0, Math.Floor((40 - text.Height) / 2), 40 - text.Height },
            laid.Take(3).Select(l => l.Text.Y - l.Picture.Y).ToArray());
        var (right, rightText) = laid[3];
        Assert.AreEqual(rightText.Right + 1, right.X);
        Assert.AreEqual(Math.Floor((200 - (16 + 1 + rightText.Width)) / 2), rightText.X);
    }

    [TestMethod]
    public void APictureAboveOrBelowTheCaption_IsOnePixelFromIt_AndTheSecondWordAlignsTheirEdges()
    {
        var laid = UI.Run(() => new[]
            {
                PicturePosition.AboveLeft, PicturePosition.AboveCenter, PicturePosition.AboveRight, PicturePosition.BelowLeft,
                PicturePosition.Center,
            }
            .Select(position => Laid(Label(200, 93, Bitmap(40, 12, Red, Green), "Ab").Set(P.PicturePosition, position))).ToArray());

        var (above, text) = laid[0];
        Assert.AreEqual(Math.Floor((200 - 40) / 2d), above.X);
        Assert.AreEqual(Math.Floor((93 - (12 + 1 + text.Height)) / 2), above.Y);
        Assert.AreEqual(above.Bottom + 1, text.Y);
        CollectionAssert.AreEqual(new[] { above.X, above.X + Math.Floor((40 - text.Width) / 2), above.Right - text.Width },
            laid.Take(3).Select(l => l.Text.X).ToArray());
        Assert.AreEqual(laid[3].Text.Bottom + 1, laid[3].Picture.Y);
        var (centre, centreText) = laid[4];
        Assert.AreEqual((Math.Floor((200 - 40) / 2d), Math.Floor((93 - 12) / 2d)), (centre.X, centre.Y));
        Assert.AreEqual(Math.Floor((200 - centreText.Width) / 2), centreText.X);
    }

    [TestMethod]
    public void APositionSetOnAShownLabel_MovesItsPicture_AndOnePastMSFormsValuesIsRefused()
    {
        var (before, after, refused) = UI.Run(() =>
        {
            var label = Label(200, 93, Bitmap(16, 12, Red, Green), "Ab").Set(P.PicturePosition, PicturePosition.LeftCenter);
            var control = new RDFormsLabel(label);
            var (window, _) = ControlHost.Show((control, new Rect(0, 0, 200, 93)));
            Dispatcher.UIThread.RunJobs();
            var before = control.PictureBounds!.Value;
            label.Set(P.PicturePosition, PicturePosition.RightCenter);
            Dispatcher.UIThread.RunJobs();
            var after = control.PictureBounds!.Value;
            window.Close();
            return (before, after, !label.TrySet(P.PicturePosition, (PicturePosition)13));
        });

        Assert.IsGreaterThan(before.X, after.X);
        Assert.IsTrue(refused);
    }

    [TestMethod]
    public void ThePictureIsAboveCenter_ByDefault_AndTheCaptionsAlignmentLeavesTheBlockWhereItIs()
    {
        var (position, laid) = UI.Run(() => (Label(200, 93, null).Get(P.PicturePosition),
            new[] { TextAlignment.LeftJustify, TextAlignment.RightJustify, TextAlignment.Center }
                .Select(alignment => Laid(Label(200, 93, Bitmap(16, 12, Red, Green), "Ab").Set(P.Alignment, alignment))).ToArray()));

        Assert.AreEqual(PicturePosition.AboveCenter, position);
        Assert.AreEqual(1, laid.Select(l => l.Picture).Distinct().Count());
        Assert.AreEqual(1, laid.Select(l => l.Text.Position).Distinct().Count());
    }

    [TestMethod]
    public void APictureLargerThanTheLabel_IsShrunkOnEachAxisAlone_AndTheBlockStartsAtTheEdge()
    {
        var laid = UI.Run(() => new (double Width, double Height)[] { (100, 33), (53, 100), (53, 33), (200, 93) }
            .Select(size => Laid(Label(size.Width, size.Height, Bitmap(64, 48, Blue, Green)).Set(P.PicturePosition, PicturePosition.Center)).Picture)
            .ToArray());

        CollectionAssert.AreEqual(new[] { new Rect(18, 0, 64, 33), new Rect(0, 26, 53, 48), new Rect(0, 0, 53, 33), new Rect(68, 22, 64, 48) },
            laid);
    }

    [TestMethod]
    public void BesideTheCaption_ThePictureLeavesItRoom_AndAboveIt_ThePictureKeepsItsHeight()
    {
        // The label is as tall as the picture, so the gap and the caption below it do not fit, whatever the font. The caption beside it is
        // on one line (AutoSize set, WordWrap not), so it is measured whole.
        var (beside, above) = UI.Run(() => (
            Laid(Label(73, 53, Bitmap(64, 48, Blue, Green), "Abcdefgh").Set(P.PicturePosition, PicturePosition.LeftCenter).Set(P.AutoSize, true)),
            Laid(Label(73, 48, Bitmap(64, 48, Blue, Green), "Ab").Set(P.PicturePosition, PicturePosition.AboveCenter))));

        Assert.IsGreaterThan(9d, beside.Text.Width);
        Assert.AreEqual(73 - beside.Text.Width - 1, beside.Picture.Width);
        Assert.AreEqual(48, beside.Picture.Height);
        Assert.AreEqual(new Rect(4, 0, 64, 48), above.Picture);
        Assert.AreEqual(49, above.Text.Y);
    }

    [TestMethod]
    public void ACaptionWiderThanTheLabel_StartsTheBlockAtItsLeftEdge()
    {
        // a caption on one line (AutoSize set, WordWrap not): one that wraps is no wider than its label.
        var (picture, text) = UI.Run(() =>
            Laid(Label(73, 60, Bitmap(16, 12, Red, Green), new string('W', 120)).Set(P.PicturePosition, PicturePosition.AboveLeft).Set(P.AutoSize, true)));

        Assert.IsGreaterThan(73d, text.Width);
        Assert.AreEqual((0d, 0d), (picture.X, text.X));
    }

    [TestMethod]
    public void ABitmapsLowerLeftColour_IsTransparent_SoABitmapOfOneColourDrawsNothing()
    {
        var (masked, plain) = UI.Run(() =>
        {
            Color[] Drawn(byte[] picture)
            {
                var control = new RDFormsLabel(Label(40, 40, picture).Set(P.PicturePosition, PicturePosition.Center));
                var (window, _) = ControlHost.Show((control, new Rect(0, 0, 40, 40)));
                var at = control.PictureBounds!.Value;
                var (x, y) = ((int)at.X, (int)at.Y);
                var pixels = PixelsAt(window, new(x + 5, y + 5), new(x, y + 9), new(x + 1, y + 9));
                window.Close();
                return pixels;
            }
            return (Drawn(Bitmap(10, 10, Red, Green)), Drawn(Bitmap(10, 10, Red, Red)));
        });

        CollectionAssert.AreEqual(new[] { Red, White, Red }, masked);
        CollectionAssert.AreEqual(new[] { White, White, White }, plain);
    }

    // The GIF's transparent pixel shows what is under the control: the form's Picture under a windowless one, a transparent label
    // included; the BackColor of a PictureBox or of an opaque label. MSForms' lower-left rule leaves a GIF as it is.
    [TestMethod]
    public void AGifsTransparentColour_ShowsTheFormsPicture_UnderAnImageOrATransparentLabel_AndTheBackColorOfAPictureBoxOrAnOpaqueLabel()
    {
        var drawn = UI.Run(() =>
        {
            var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 200d).Set(P.Height, 60d).Set(P.ScaleMode, ScaleMode.Pixel)
                .Set(P.Picture, Bitmap(200, 60, Red, Red));
            var yellow = FormColor.FromRgb(255, 255, 0);
            ComponentInstance Add(ComponentClass type, string name, double left)
            {
                var control = new ComponentInstance(type, name).Set(P.Left, left).Set(P.Top, 10d).Set(P.Width, 20d).Set(P.Height, 20d)
                    .Set(P.Picture, ClearGif);
                control.SetContainer(root);
                return control;
            }
            Add(ComponentClasses.Image, "Image1", 10);
            Add(ComponentClasses.PictureBox, "Picture1", 50).Set(P.BackColor, yellow).Set(P.BorderStyle, BorderStyle.None);
            Add(ComponentClasses.Label, "Clear", 90).Set(P.BackStyle, BackStyle.Transparent).Set(P.PicturePosition, PicturePosition.AboveLeft);
            Add(ComponentClasses.Label, "Opaque", 130).Set(P.BackColor, yellow).Set(P.PicturePosition, PicturePosition.AboveLeft);
            var window = new RDFormsFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var origin = window.View.TranslatePoint(default, window)!.Value;
            var pixels = new[] { 10, 50, 90, 130 }.Select(left =>
            {
                var control = window.View.ControlOf(root.Contents.Single(c => c.Get(P.Left) == left))!;
                var inside = control is RDFormsLabel label ? label.PictureBounds!.Value.Position : default;
                var (x, y) = ((int)(origin.X + left + inside.X), (int)(origin.Y + 10 + inside.Y));
                return PixelsAt(window, new(x, y), new(x + 1, y), new(x, y + 1));
            }).ToArray();
            window.Close();
            return pixels;
        });

        CollectionAssert.AreEqual(new[] { Red, Blue, Green }, drawn[0], "Image");
        CollectionAssert.AreEqual(new[] { Yellow, Blue, Green }, drawn[1], "PictureBox");
        CollectionAssert.AreEqual(new[] { Red, Blue, Green }, drawn[2], "transparent Label");
        CollectionAssert.AreEqual(new[] { Yellow, Blue, Green }, drawn[3], "opaque Label");
    }

    // VB shares one picture object between the controls it is set on: the same bytes are decoded once for any number of controls, and
    // again only once none of them holds them.
    [TestMethod]
    public void TheSamePictureOnManyControls_IsDecodedOnce_AndAgainOnlyOnceNoneHoldsIt()
    {
        var (shared, copy, stillDrawn, again) = UI.Run(() =>
        {
            var picture = Bitmap(8, 8, Red, Green);
            var labels = Enumerable.Range(0, 100).Select(_ => Label(20, 20, null)).ToArray();
            var controls = labels.Select(label => new RDFormsLabel(label)).ToArray();
            var (window, _) = ControlHost.Show([.. controls.Select((control, i) => ((Control)control, new Rect(i % 10 * 20, i / 10 * 20, 20, 20)))]);
            var before = RDFormsPicture.DecodesOnThisThread;
            foreach (var label in labels)
                label.Set(P.Picture, picture);
            var shared = RDFormsPicture.DecodesOnThisThread - before;
            labels[0].Set(P.Picture, (byte[])picture.Clone());
            var copy = RDFormsPicture.DecodesOnThisThread - before - shared;
            // The others still draw the picture the first one let go of.
            Dispatcher.UIThread.RunJobs();
            var at = controls[5].PictureBounds!.Value;
            var stillDrawn = PixelsAt(window, new PixelPoint((int)(100 + at.X + 4), (int)(at.Y + 4)))[0];
            foreach (var label in labels)
                label.Set(P.Picture, null);
            labels[1].Set(P.Picture, picture);
            var again = RDFormsPicture.DecodesOnThisThread - before - shared - copy;
            window.Close();
            return (shared, copy, stillDrawn, again);
        });

        Assert.AreEqual(1, shared);
        Assert.AreEqual(1, copy);
        Assert.AreEqual(Red, stillDrawn);
        Assert.AreEqual(1, again);
    }
}

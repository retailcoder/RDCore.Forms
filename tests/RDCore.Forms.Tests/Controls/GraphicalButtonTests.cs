using System.Buffers.Binary;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Tests.Model;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A button's Picture and Style Graphical, as measured with VB6, native and p-code agreeing: a Standard button draws no picture and
// paints no BackColor; a Graphical one paints its BackColor, centres its picture over its caption, and is drawn one pixel right and
// down while it is down, with its DownPicture; a disabled one shows its DisabledPicture.
[TestClass]
public sealed class GraphicalButtonTests
{
    private static readonly Color Red = Color.FromRgb(255, 0, 0), Blue = Color.FromRgb(0, 0, 255), Green = Color.FromRgb(0, 255, 0);
    private static readonly FormColor Yellow = FormColor.FromRgb(255, 255, 0);

    // A 16x16 picture of one colour, as premultiplied BGRA.
    private static byte[] Solid(uint bgra) => PictureControlTests.Png(16, 16, [.. Enumerable.Repeat(bgra, 256)]);

    private static readonly byte[] RedPicture = Solid(0xFFFF0000), BluePicture = Solid(0xFF0000FF), GreenPicture = Solid(0xFF00FF00);

    private static ComponentInstance Button(ComponentClass type, ButtonStyle style, string caption = "Go") =>
        new ComponentInstance(type, "Button1").Set(P.ButtonStyle, style).Set(P.Caption, caption).Set(P.Picture, RedPicture);

    private static Control Make(ComponentInstance instance) => instance.Class.TypeName switch
    {
        "RDForms.CommandButton" => new RDFormsCommandButton(instance),
        "RDForms.CheckBox" => new RDFormsCheckBox(instance),
        _ => new RDFormsOptionButton(instance),
    };

    // The pixels of a colour in a control, or in an area of it, as their bounding box from the control's top left corner, or null where
    // there are none.
    private static PixelRect? Box(Window window, Control control, Color colour, PixelRect? within = null)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var origin = control.TranslatePoint(default, window)!.Value;
        var (left, top) = ((int)origin.X, (int)origin.Y);
        var area = within ?? new PixelRect(0, 0, (int)control.Bounds.Width, (int)control.Bounds.Height);
        var points = (from y in Enumerable.Range(top + area.Y, area.Height) from x in Enumerable.Range(left + area.X, area.Width)
                      select new PixelPoint(x, y)).ToArray();
        var hits = points.Zip(PictureControlTests.PixelsAt(window, points)).Where(p => p.Second == colour).Select(p => p.First).ToList();
        if (hits.Count == 0)
            return null;
        var (x0, y0) = (hits.Min(p => p.X), hits.Min(p => p.Y));
        return new PixelRect(x0 - left, y0 - top, hits.Max(p => p.X) - x0 + 1, hits.Max(p => p.Y) - y0 + 1);
    }

    private static TextBlock Caption(Control button) => button.GetVisualDescendants().OfType<TextBlock>().Single();

    // The opacity a visual is drawn with, from the button down: the product of its own and of its ancestors' up to the button.
    private static double OpacityOf(Visual visual, Control button) =>
        visual.GetSelfAndVisualAncestors().TakeWhile(v => v != button.GetVisualParent()).Aggregate(1.0, (opacity, v) => opacity * v.Opacity);

    private static Rect CaptionBounds(Control button) =>
        button.GetVisualDescendants().OfType<TextBlock>().Single() is var text ? text.Bounds.WithY(text.TranslatePoint(default, button)!.Value.Y) : default;

    // A press or a release of the left button 4 pixels in from the button's top left corner, through the window's input as the
    // user's: it renders the window first, so that the release's hit test finds the button and it clicks.
    private static void Down(Control button, bool down)
    {
        var root = TopLevel.GetTopLevel(button)!;
        var point = button.TranslatePoint(new Point(4, 4), root)!.Value;
        if (down)
            root.MouseDown(point, MouseButton.Left);
        else
            root.MouseUp(point, MouseButton.Left);
    }

    private static readonly Rect Cell = new(10, 10, 100, 60);

    [TestMethod]
    [DataRow("RDForms.CommandButton")]
    [DataRow("RDForms.CheckBox")]
    [DataRow("RDForms.OptionButton")]
    public void AStandardButton_DrawsNoPicture(string type)
    {
        var red = UI.Run(() =>
        {
            var control = Make(Button(ComponentClasses.All.Single(c => c.TypeName == type), ButtonStyle.Standard));
            var (window, _) = ControlHost.Show((control, Cell));
            var result = Box(window, control, Red);
            window.Close();
            return result;
        });

        Assert.IsNull(red);
    }

    [TestMethod]
    public void AStandardCommandButton_PaintsNoBackColor_AndAGraphicalOneDoes()
    {
        var (standard, graphical) = UI.Run(() =>
        {
            var plain = new RDFormsCommandButton(Button(ComponentClasses.CommandButton, ButtonStyle.Standard).Set(P.BackColor, Yellow));
            var drawn = new RDFormsCommandButton(Button(ComponentClasses.CommandButton, ButtonStyle.Graphical).Set(P.BackColor, Yellow));
            var (window, _) = ControlHost.Show((plain, Cell), (drawn, Cell.Translate(new Vector(120, 0))));
            var yellow = Color.FromRgb(255, 255, 0);
            var result = (Box(window, plain, yellow), Box(window, drawn, yellow));
            window.Close();
            return result;
        });

        Assert.IsNull(standard);
        Assert.IsNotNull(graphical);
    }

    [TestMethod]
    [DataRow("RDForms.CommandButton")]
    [DataRow("RDForms.CheckBox")]
    [DataRow("RDForms.OptionButton")]
    public void AGraphicalButton_CentresItsPictureAcross_AboveItsCaption_AtTheBottom(string type)
    {
        var (red, caption) = UI.Run(() =>
        {
            var control = Make(Button(ComponentClasses.All.Single(c => c.TypeName == type), ButtonStyle.Graphical));
            var (window, _) = ControlHost.Show((control, Cell));
            var result = (Box(window, control, Red), CaptionBounds(control));
            window.Close();
            return result;
        });

        Assert.AreEqual(new PixelRect(42, red!.Value.Y, 16, 16), red);
        Assert.IsTrue(red.Value.Bottom <= caption.Top, $"The picture ends at {red.Value.Bottom}, under the caption's top at {caption.Top}.");
        Assert.IsTrue(caption.Bottom >= 50, $"The caption ends at {caption.Bottom}, above the bottom of the button.");
        Assert.IsTrue(red.Value.Y > 0, "The picture starts at the top of the button.");
    }

    // A VB6 form's Graphical button, with no PicturePosition, keeps VB6's layout: in a tall button its caption is still at the bottom,
    // where MSForms' layout would centre the picture and the caption together.
    [TestMethod]
    [DataRow("RDForms.CommandButton")]
    [DataRow("RDForms.CheckBox")]
    [DataRow("RDForms.OptionButton")]
    public void AVB6GraphicalButton_KeepsItsCaptionAtTheBottom_InATallButton(string type)
    {
        var (red, caption) = UI.Run(() =>
        {
            var control = Make(Button(ComponentClasses.All.Single(c => c.TypeName == type), ButtonStyle.Graphical));
            var (window, _) = ControlHost.Show((control, new Rect(10, 10, 100, 100)));
            var result = (Box(window, control, Red), CaptionBounds(control));
            window.Close();
            return result;
        });

        Assert.AreEqual(new PixelRect(42, red!.Value.Y, 16, 16), red);
        Assert.IsTrue(caption.Bottom >= 90, $"The caption ends at {caption.Bottom}, above the bottom of the button.");
        Assert.IsTrue(red.Value.Bottom <= caption.Top, $"The picture ends at {red.Value.Bottom}, under the caption's top at {caption.Top}.");
    }

    [TestMethod]
    public void AGraphicalButtonWithNoCaption_CentresItsPicture()
    {
        var red = UI.Run(() =>
        {
            var control = new RDFormsCommandButton(Button(ComponentClasses.CommandButton, ButtonStyle.Graphical, caption: ""));
            var (window, _) = ControlHost.Show((control, Cell));
            var result = Box(window, control, Red);
            window.Close();
            return result;
        });

        Assert.AreEqual(new PixelRect(42, 22, 16, 16), red);
    }

    [TestMethod]
    public void AGraphicalCommandButton_HeldDown_MovesItsFaceOnePixel_AndShowsItsDownPicture_ThenComesBack()
    {
        var (up, held, heldDown, released) = UI.Run(() =>
        {
            var instance = Button(ComponentClasses.CommandButton, ButtonStyle.Graphical);
            var plain = new RDFormsCommandButton(instance);
            var withDown = new RDFormsCommandButton(Button(ComponentClasses.CommandButton, ButtonStyle.Graphical).Set(P.DownPicture, BluePicture));
            var (window, _) = ControlHost.Show((plain, Cell), (withDown, Cell.Translate(new Vector(120, 0))));
            var up = Box(window, plain, Red);
            Down(plain, true);
            var held = Box(window, plain, Red);
            Down(plain, false);
            var released = Box(window, plain, Red);
            Down(withDown, true);
            var heldDown = (Box(window, withDown, Red), Box(window, withDown, Blue));
            Down(withDown, false);
            window.Close();
            return (up, held, heldDown, released);
        });

        Assert.AreEqual(up!.Value.Translate(new PixelVector(1, 1)), held);
        Assert.AreEqual(up, released);
        Assert.AreEqual((null, up.Value.Translate(new PixelVector(1, 1))), heldDown);
    }

    [TestMethod]
    public void ADisabledGraphicalButton_ShowsItsDisabledPicture_OrItsPictureGreyed()
    {
        var (withDisabled, greyed, (back, captionOpacity)) = UI.Run(() =>
        {
            var one = new RDFormsCommandButton(Button(ComponentClasses.CommandButton, ButtonStyle.Graphical).Set(P.DisabledPicture, GreenPicture)
                .Set(P.Enabled, false));
            var other = new RDFormsCommandButton(Button(ComponentClasses.CommandButton, ButtonStyle.Graphical).Set(P.BackColor, Yellow)
                .Set(P.Enabled, false));
            var (window, _) = ControlHost.Show((one, Cell), (other, Cell.Translate(new Vector(120, 0))));
            var result = ((Box(window, one, Red), Box(window, one, Green)), Box(window, other, Red),
                (Box(window, other, Color.FromRgb(255, 255, 0)), OpacityOf(Caption(other), other)));
            window.Close();
            return result;
        });

        Assert.IsNull(withDisabled.Item1);
        Assert.AreEqual(42, withDisabled.Item2?.X);
        Assert.IsNull(greyed);
        Assert.IsNotNull(back);
        Assert.AreEqual(0.5, captionOpacity);
    }

    [TestMethod]
    public void AGraphicalButton_GreysItsCaption_OnlyWhileDisabled()
    {
        var (disabled, enabled) = UI.Run(() =>
        {
            var instance = Button(ComponentClasses.CommandButton, ButtonStyle.Graphical).Set(P.Enabled, false);
            var button = new RDFormsCommandButton(instance);
            var (window, _) = ControlHost.Show((button, Cell));
            var disabled = OpacityOf(Caption(button), button);
            instance.Set(P.Enabled, true);
            Dispatcher.UIThread.RunJobs();
            var result = (disabled, OpacityOf(Caption(button), button));
            window.Close();
            return result;
        });

        Assert.AreEqual(0.5, disabled);
        Assert.AreEqual(1.0, enabled);
    }

    [TestMethod]
    public void AGraphicalCheckBox_IsDownWhileChecked_WithItsDownPicture_AndUpWhenGrayed()
    {
        var (cleared, checkedBox, grayed, withDown) = UI.Run(() =>
        {
            var instance = Button(ComponentClasses.CheckBox, ButtonStyle.Graphical);
            var box = new RDFormsCheckBox(instance);
            var downInstance = Button(ComponentClasses.CheckBox, ButtonStyle.Graphical).Set(P.DownPicture, BluePicture);
            var other = new RDFormsCheckBox(downInstance);
            var (window, _) = ControlHost.Show((box, Cell), (other, Cell.Translate(new Vector(120, 0))));
            var cleared = Box(window, box, Red);
            instance.Set(P.CheckBoxValue, CheckBoxValue.Checked);
            var checkedBox = Box(window, box, Red);
            instance.Set(P.CheckBoxValue, CheckBoxValue.Grayed);
            var grayed = Box(window, box, Red);
            downInstance.Set(P.CheckBoxValue, CheckBoxValue.Checked);
            var withDown = Box(window, other, Blue);
            window.Close();
            return (cleared, checkedBox, grayed, withDown);
        });

        Assert.AreEqual(cleared!.Value.Translate(new PixelVector(1, 1)), checkedBox);
        Assert.AreEqual(cleared, grayed);
        Assert.AreEqual(checkedBox, withDown);
    }

    [TestMethod]
    public void AGraphicalCheckBox_StillTogglesAndClicks()
    {
        var (value, raised) = UI.Run(() =>
        {
            var instance = Button(ComponentClasses.CheckBox, ButtonStyle.Graphical);
            var box = new RDFormsCheckBox(instance);
            var (window, sink) = ControlHost.Show((box, Cell));
            Down(box, true);
            Down(box, false);
            var result = (instance.Get(P.CheckBoxValue), sink.WithoutMoves.Where(e => e.EndsWith(".Click")).ToList());
            window.Close();
            return result;
        });

        Assert.AreEqual(CheckBoxValue.Checked, value);
        Expect.Lines(["Button1.Click"], raised);
    }

    [TestMethod]
    public void AGraphicalOptionButton_IsDownWhileSelected_AndUpAgainWhenAnotherIs()
    {
        var (up, selected, cleared) = UI.Run(() =>
        {
            var first = Button(ComponentClasses.OptionButton, ButtonStyle.Graphical);
            var second = new ComponentInstance(ComponentClasses.OptionButton, "Option2");
            var option = new RDFormsOptionButton(first);
            var (window, _) = ControlHost.Show((option, Cell), (new RDFormsOptionButton(second), Cell.Translate(new Vector(120, 0))));
            var up = Box(window, option, Red);
            first.Set(P.OptionButtonValue, true);
            var selected = Box(window, option, Red);
            second.Set(P.OptionButtonValue, true);
            var cleared = Box(window, option, Red);
            window.Close();
            return (up, selected, cleared);
        });

        Assert.AreEqual(up!.Value.Translate(new PixelVector(1, 1)), selected);
        Assert.AreEqual(up, cleared);
    }

    private static readonly Color Silver = Color.FromRgb(0xC0, 0xC0, 0xC0), Magenta = Color.FromRgb(255, 0, 255);

    // Where a captionless button's 16x16 picture is drawn.
    private static readonly PixelRect PictureArea = new(42, 22, 16, 16);

    // A 16x16 24-bit bitmap as VB6's MaskColor was measured with: a border 4 pixels wide of one colour around an 8x8 centre of another.
    private static byte[] Bordered(Color border, Color centre)
    {
        var bytes = new byte[54 + 16 * 16 * 3];
        "BM"u8.CopyTo(bytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(2), bytes.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(10), 54);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(18), 16);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(22), 16);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(26), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(28), 24);
        for (var i = 0; i < 256; i++)
        {
            var colour = IsCentre(i) ? centre : border;
            (bytes[54 + i * 3], bytes[55 + i * 3], bytes[56 + i * 3]) = (colour.B, colour.G, colour.R);
        }
        return bytes;
    }

    private static bool IsCentre(int pixel) => pixel % 16 is >= 4 and < 12 && pixel / 16 is >= 4 and < 12;

    // The same picture as an icon (1) or a cursor (2) holding one PNG image.
    private static byte[] BorderedIcon(short type, Color border, Color centre) => TestPictures.Icon(type, PictureControlTests.Png(16, 16,
        [.. Enumerable.Range(0, 256).Select(i => IsCentre(i) ? centre : border).Select(c => 0xFF000000u | (uint)c.R << 16 | (uint)c.G << 8 | c.B)]));

    private static ComponentInstance Masked(ComponentClass type, byte[] picture, bool useMaskColor = true) =>
        Button(type, ButtonStyle.Graphical, caption: "").Set(P.BackColor, Yellow).Set(P.Picture, picture).Set(P.UseMaskColor, useMaskColor);

    [TestMethod]
    [DataRow("RDForms.CommandButton")]
    [DataRow("RDForms.CheckBox")]
    [DataRow("RDForms.OptionButton")]
    public void UseMaskColor_LeavesTheMaskColorOutOfThePicture_SilverByDefault(string type)
    {
        var (masked, unmasked) = UI.Run(() =>
        {
            var kind = ComponentClasses.All.Single(c => c.TypeName == type);
            var one = Make(Masked(kind, Bordered(Silver, Red)));
            var other = Make(Masked(kind, Bordered(Silver, Red), useMaskColor: false));
            var (window, _) = ControlHost.Show((one, Cell), (other, Cell.Translate(new Vector(120, 0))));
            var result = ((Box(window, one, Silver), Box(window, one, Red), Box(window, one, Color.FromRgb(255, 255, 0), PictureArea)),
                Box(window, other, Silver));
            window.Close();
            return result;
        });

        Assert.AreEqual((null, new PixelRect(46, 26, 8, 8), PictureArea), masked, "the border shows the face, BackColor");
        Assert.AreEqual(PictureArea, unmasked);
    }

    [TestMethod]
    public void TheMask_IsExactlyTheMaskColor()
    {
        var (near, magenta, other) = UI.Run(() =>
        {
            var c1c0c0 = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, Bordered(Silver, Red)).Set(P.MaskColor, FormColor.FromRgb(0xC1, 0xC0, 0xC0)));
            var onMagenta = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, Bordered(Magenta, Red)).Set(P.MaskColor, FormColor.FromRgb(255, 0, 255)));
            var onSilver = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, Bordered(Silver, Red)).Set(P.MaskColor, FormColor.FromRgb(255, 0, 255)));
            var (window, _) = ControlHost.Show((c1c0c0, Cell), (onMagenta, Cell.Translate(new Vector(120, 0))), (onSilver, Cell.Translate(new Vector(240, 0))));
            var result = (Box(window, c1c0c0, Silver), Box(window, onMagenta, Magenta), Box(window, onSilver, Silver));
            window.Close();
            return result;
        });

        Assert.AreEqual(PictureArea, near, "C1C0C0 is not C0C0C0");
        Assert.IsNull(magenta);
        Assert.AreEqual(PictureArea, other);
    }

    [TestMethod]
    public void AMaskColorThatIsASystemColour_IsTheColourItStandsFor()
    {
        var face = FormPalette.ToColor(FormColor.FromSystem(SystemColor.ButtonFace));
        var border = UI.Run(() =>
        {
            var button = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, Bordered(face, Red)).Set(P.MaskColor, FormColor.FromSystem(SystemColor.ButtonFace)));
            var (window, _) = ControlHost.Show((button, Cell));
            var result = Box(window, button, face, PictureArea);
            window.Close();
            return result;
        });

        Assert.IsNull(border);
    }

    [TestMethod]
    public void TheMask_IsLeftOutOfTheDownPicture_AndOfTheDisabledPicture()
    {
        var (held, disabled) = UI.Run(() =>
        {
            var one = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, Bordered(Silver, Red)).Set(P.DownPicture, Bordered(Silver, Blue)));
            var other = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, Bordered(Silver, Red)).Set(P.DisabledPicture, Bordered(Silver, Green))
                .Set(P.Enabled, false));
            var (window, _) = ControlHost.Show((one, Cell), (other, Cell.Translate(new Vector(120, 0))));
            Down(one, true);
            var held = (Box(window, one, Silver), Box(window, one, Blue));
            Down(one, false);
            var result = (held, (Box(window, other, Silver), Box(window, other, Green)));
            window.Close();
            return result;
        });

        Assert.AreEqual((null, new PixelRect(47, 27, 8, 8)), held);
        Assert.AreEqual((null, new PixelRect(46, 26, 8, 8)), disabled);
    }

    [TestMethod]
    public void UseMaskColorAndMaskColor_SetAtRunTime_RedrawTheButton()
    {
        var (before, masked, remasked) = UI.Run(() =>
        {
            var instance = Masked(ComponentClasses.CommandButton, Bordered(Silver, Red), useMaskColor: false);
            var button = new RDFormsCommandButton(instance);
            var (window, _) = ControlHost.Show((button, Cell));
            var before = Box(window, button, Silver);
            instance.Set(P.UseMaskColor, true);
            var masked = Box(window, button, Silver);
            instance.Set(P.MaskColor, FormColor.FromRgb(255, 0, 255));
            var result = (before, masked, Box(window, button, Silver));
            window.Close();
            return result;
        });

        Assert.AreEqual(PictureArea, before);
        Assert.IsNull(masked);
        Assert.AreEqual(PictureArea, remasked);
    }

    [TestMethod]
    [DataRow((short)1, DisplayName = "an icon")]
    [DataRow((short)2, DisplayName = "a cursor")]
    public void AnIconOrACursor_IsDrawnWhole_WithUseMaskColor(short type)
    {
        var silver = UI.Run(() =>
        {
            var button = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, BorderedIcon(type, Silver, Red)));
            var (window, _) = ControlHost.Show((button, Cell));
            var result = Box(window, button, Silver);
            window.Close();
            return result;
        });

        Assert.AreEqual(PictureArea, silver);
    }

    // A PNG, which VB6 does not load, is masked as a bitmap: only its opaque pixels of the mask colour are left out, and a pixel of that
    // colour that is half transparent is drawn blended over the face.
    [TestMethod]
    public void UseMaskColor_OnAPng_LeavesOutItsOpaquePixelsOfTheMaskColor_AndBlendsItsHalfTransparentOnes()
    {
        var (silver, border, centre) = UI.Run(() =>
        {
            // An opaque silver border around a centre of silver at half its alpha, premultiplied.
            var png = PictureControlTests.Png(16, 16, [.. Enumerable.Range(0, 256).Select(i => IsCentre(i) ? 0x80606060u : 0xFFC0C0C0u)]);
            var button = new RDFormsCommandButton(Masked(ComponentClasses.CommandButton, png));
            var (window, _) = ControlHost.Show((button, Cell));
            var corner = new PixelPoint((int)Cell.X + PictureArea.X, (int)Cell.Y + PictureArea.Y);
            var pixels = PictureControlTests.PixelsAt(window, corner + new PixelVector(1, 1), corner + new PixelVector(8, 8));
            var result = (Box(window, button, Silver), pixels[0], pixels[1]);
            window.Close();
            return result;
        });

        Assert.IsNull(silver);
        Assert.AreEqual(Color.FromRgb(255, 255, 0), border, "the border shows the face, BackColor");
        Assert.IsTrue(centre is { R: >= 216 and <= 232, G: >= 216 and <= 232, B: >= 88 and <= 104 }, centre.ToString());
    }

    [TestMethod]
    public void StyleSetAfterTheButtonIsMade_IsIgnored()
    {
        var red = UI.Run(() =>
        {
            var instance = Button(ComponentClasses.CommandButton, ButtonStyle.Standard);
            var control = new RDFormsCommandButton(instance);
            var (window, _) = ControlHost.Show((control, Cell));
            instance.Set(P.ButtonStyle, ButtonStyle.Graphical);
            var result = Box(window, control, Red);
            window.Close();
            return result;
        });

        Assert.IsNull(red);
    }
}

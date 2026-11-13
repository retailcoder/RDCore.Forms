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
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
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
        "VB.CommandButton" => new VBCommandButton(instance),
        "VB.CheckBox" => new VBCheckBox(instance),
        _ => new VBOptionButton(instance),
    };

    // The pixels of a colour in a control, as their bounding box from the control's top left corner, or null where there are none.
    private static PixelRect? Box(Window window, Control control, Color colour)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        var origin = control.TranslatePoint(default, window)!.Value;
        var (left, top) = ((int)origin.X, (int)origin.Y);
        var (width, height) = ((int)control.Bounds.Width, (int)control.Bounds.Height);
        var points = (from y in Enumerable.Range(top, height) from x in Enumerable.Range(left, width) select new PixelPoint(x, y)).ToArray();
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

    private static void Down(Control button, bool down)
    {
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, isPrimary: true);
        var root = TopLevel.GetTopLevel(button)!;
        var point = button.TranslatePoint(new Point(4, 4), root)!.Value;
        if (down)
            button.RaiseEvent(new PointerPressedEventArgs(button, pointer, root, point, 0,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None, 1));
        else
            button.RaiseEvent(new PointerReleasedEventArgs(button, pointer, root, point, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased), KeyModifiers.None, MouseButton.Left));
        Dispatcher.UIThread.RunJobs();
    }

    private static readonly Rect Cell = new(10, 10, 100, 60);

    [TestMethod]
    [DataRow("VB.CommandButton")]
    [DataRow("VB.CheckBox")]
    [DataRow("VB.OptionButton")]
    public void AStandardButton_DrawsNoPicture(string type)
    {
        var red = UI.Run(() =>
        {
            var control = Make(Button(VBClasses.All.Single(c => c.TypeName == type), ButtonStyle.Standard));
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
            var plain = new VBCommandButton(Button(VBClasses.CommandButton, ButtonStyle.Standard).Set(P.BackColor, Yellow));
            var drawn = new VBCommandButton(Button(VBClasses.CommandButton, ButtonStyle.Graphical).Set(P.BackColor, Yellow));
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
    [DataRow("VB.CommandButton")]
    [DataRow("VB.CheckBox")]
    [DataRow("VB.OptionButton")]
    public void AGraphicalButton_CentresItsPictureAcross_AboveItsCaption_AtTheBottom(string type)
    {
        var (red, caption) = UI.Run(() =>
        {
            var control = Make(Button(VBClasses.All.Single(c => c.TypeName == type), ButtonStyle.Graphical));
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

    [TestMethod]
    public void AGraphicalButtonWithNoCaption_CentresItsPicture()
    {
        var red = UI.Run(() =>
        {
            var control = new VBCommandButton(Button(VBClasses.CommandButton, ButtonStyle.Graphical, caption: ""));
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
            var instance = Button(VBClasses.CommandButton, ButtonStyle.Graphical);
            var plain = new VBCommandButton(instance);
            var withDown = new VBCommandButton(Button(VBClasses.CommandButton, ButtonStyle.Graphical).Set(P.DownPicture, BluePicture));
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
            var one = new VBCommandButton(Button(VBClasses.CommandButton, ButtonStyle.Graphical).Set(P.DisabledPicture, GreenPicture)
                .Set(P.Enabled, false));
            var other = new VBCommandButton(Button(VBClasses.CommandButton, ButtonStyle.Graphical).Set(P.BackColor, Yellow)
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
            var instance = Button(VBClasses.CommandButton, ButtonStyle.Graphical).Set(P.Enabled, false);
            var button = new VBCommandButton(instance);
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
            var instance = Button(VBClasses.CheckBox, ButtonStyle.Graphical);
            var box = new VBCheckBox(instance);
            var downInstance = Button(VBClasses.CheckBox, ButtonStyle.Graphical).Set(P.DownPicture, BluePicture);
            var other = new VBCheckBox(downInstance);
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
            var instance = Button(VBClasses.CheckBox, ButtonStyle.Graphical);
            var box = new VBCheckBox(instance);
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
            var first = Button(VBClasses.OptionButton, ButtonStyle.Graphical);
            var second = new ComponentInstance(VBClasses.OptionButton, "Option2");
            var option = new VBOptionButton(first);
            var (window, _) = ControlHost.Show((option, Cell), (new VBOptionButton(second), Cell.Translate(new Vector(120, 0))));
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

    [TestMethod]
    public void StyleSetAfterTheButtonIsMade_IsIgnored()
    {
        var red = UI.Run(() =>
        {
            var instance = Button(VBClasses.CommandButton, ButtonStyle.Standard);
            var control = new VBCommandButton(instance);
            var (window, _) = ControlHost.Show((control, Cell));
            instance.Set(P.ButtonStyle, ButtonStyle.Graphical);
            var result = Box(window, control, Red);
            window.Close();
            return result;
        });

        Assert.IsNull(red);
    }
}

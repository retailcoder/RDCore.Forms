using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using static RDCore.Forms.Tests.Controls.PictureControlTests;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A control's Opacity fades the whole control, with the controls it holds, over what is under it.
[TestClass]
public sealed class OpacityControlTests
{
    private static readonly Color White = Color.FromRgb(255, 255, 255);
    private static readonly Color Red = Color.FromRgb(255, 0, 0);
    private static readonly Color Blue = Color.FromRgb(0, 0, 255);

    // A white form holding a borderless red PictureBox at 10, 10 of 100 by 60 pixels, which holds a blue Label at 50, 10 of 40 by 30.
    private static (ComponentInstance Root, ComponentInstance Picture, ComponentInstance Label) Form()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 300d).Set(P.Height, 200d).Set(P.BackColor, FormColor.FromRgb(255, 255, 255));
        var picture = new ComponentInstance(ComponentClasses.PictureBox, "Picture1").Set(P.Left, 10d).Set(P.Top, 10d).Set(P.Width, 100d).Set(P.Height, 60d)
            .Set(P.BorderStyle, BorderStyle.None).Set(P.BackColor, FormColor.FromRgb(255, 0, 0));
        picture.SetContainer(root);
        var label = new ComponentInstance(ComponentClasses.Label, "Label1").Set(P.Left, 50d).Set(P.Top, 10d).Set(P.Width, 40d).Set(P.Height, 30d)
            .Set(P.Caption, "").Set(P.BackColor, FormColor.FromRgb(0, 0, 255));
        label.SetContainer(picture);
        return (root, picture, label);
    }

    // The picture's own pixel at 20, 20 and its label's at 70, 30, with the picture at each opacity in turn.
    private static (Color Picture, Color Label)[] Rendered(params double[] opacities) => UI.Run(() =>
    {
        var (root, picture, _) = Form();
        var window = new RDFormsFormWindow(new FormDefinition(root));
        window.Show();
        var colours = opacities.Select(opacity =>
        {
            picture.Set(P.Opacity, opacity);
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var pixels = PixelsAt(window, new(20, 20), new(70, 30));
            return (pixels[0], pixels[1]);
        }).ToArray();
        window.Close();
        return colours;
    });

    private static void AssertNear(Color expected, Color actual, string message)
    {
        Assert.AreEqual(expected.R, actual.R, 2d, message);
        Assert.AreEqual(expected.G, actual.G, 2d, message);
        Assert.AreEqual(expected.B, actual.B, 2d, message);
    }

    [TestMethod]
    public void AContainersOpacity_FadesIt_AndTheControlsItHolds_OverTheForm()
    {
        var rendered = Rendered(1, 0.5, 0, 1);

        AssertNear(Red, rendered[0].Picture, "1, picture");
        AssertNear(Blue, rendered[0].Label, "1, label");
        AssertNear(Color.FromRgb(255, 128, 128), rendered[1].Picture, "0.5, picture");
        AssertNear(Color.FromRgb(128, 128, 255), rendered[1].Label, "0.5, label");
        AssertNear(White, rendered[2].Picture, "0, picture");
        AssertNear(White, rendered[2].Label, "0, label");
        AssertNear(Red, rendered[3].Picture, "1 again, picture");
        AssertNear(Blue, rendered[3].Label, "1 again, label");
    }

    [TestMethod]
    public void AControlsOpacity_IsAMaskOverIt_AndAHeldControlHasNoneOfItsOwn()
    {
        var (faded, held, solid) = UI.Run(() =>
        {
            var (root, picture, label) = Form();
            var window = new RDFormsFormWindow(new FormDefinition(root));
            window.Show();
            Dispatcher.UIThread.RunJobs();
            picture.Set(P.Opacity, 0.25);
            var faded = window.View.ControlOf(picture)!.OpacityMask is ISolidColorBrush { Opacity: 0.25 };
            var held = window.View.ControlOf(label)!.OpacityMask;
            picture.Set(P.Opacity, 1d);
            var solid = window.View.ControlOf(picture)!.OpacityMask;
            window.Close();
            return (faded, held, solid);
        });

        Assert.IsTrue(faded);
        Assert.IsNull(held);
        Assert.IsNull(solid);
    }

    // a MultiPage's page fades the controls it holds, as a Frame does.
    [TestMethod]
    public void APagesOpacity_IsAMaskOverIt()
    {
        var masked = UI.Run(() =>
            new RDFormsPage(new ComponentInstance(ComponentClasses.Page, "Page1").Set(P.Opacity, 0.25)).OpacityMask is ISolidColorBrush { Opacity: 0.25 });

        Assert.IsTrue(masked);
    }

    // Only Visible False takes a control out of the mouse's way.
    [TestMethod]
    public void AControlAtOpacity0_StillRaisesItsMouseEvents()
    {
        var raised = UI.Run(() =>
        {
            var instance = new ComponentInstance(ComponentClasses.CommandButton, "Command1");
            var (window, sink) = ControlHost.Show(new RDFormsCommandButton(instance));
            instance.Set(P.Opacity, 0d);
            Dispatcher.UIThread.RunJobs();
            window.MouseDown(new Point(20, 10), MouseButton.Left);
            window.MouseUp(new Point(20, 10), MouseButton.Left);
            window.Close();
            return sink.WithoutMoves;
        });

        Expect.Lines(new[]
        {
            "Command1.GotFocus",
            "Command1.MouseDown(Integer 1, Integer 0, Single 150, Single 75)",
            "Command1.Click",
            "Command1.MouseUp(Integer 1, Integer 0, Single 150, Single 75)",
        }, raised);
    }
}

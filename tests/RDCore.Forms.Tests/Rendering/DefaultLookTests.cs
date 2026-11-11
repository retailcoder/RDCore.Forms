using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// The button Enter clicks has the default look: the default button, while no CommandButton has the focus, as measured with VB6; and the
// focused CommandButton. The look itself is RDForms' own.
[TestClass]
public sealed class DefaultLookTests
{
    // A form holding a control of each class named, one under the other, each named for its class and its place (TextBox1, CommandButton2).
    private static ComponentInstance Form(params ComponentClass[] classes)
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
        var top = 8d;
        foreach (var (type, index) in classes.Select((type, index) => (type, index)))
        {
            var control = new ComponentInstance(type, $"{type.ClassName}{index + 1}").Set(P.Left, 8d).Set(P.Top, top)
                .Set(P.Width, 120d).Set(P.Height, 25d);
            control.SetContainer(root);
            top += 33;
        }
        return root;
    }

    private static VBFormWindow Open(ComponentInstance root)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, new RecordingSink());
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // The buttons that have the default look, by name.
    private static string Looks(VBFormWindow window) =>
        string.Join(", ", window.Root.Contents.Where(c => window.View.ControlOf(c) is Button button && button.Classes.Contains(":default"))
            .Select(c => c.Name));

    private static void Focus(VBFormWindow window, ComponentInstance component)
    {
        window.View.ControlOf(component)!.Focus();
        Dispatcher.UIThread.RunJobs();
    }

    [TestMethod]
    public void TheDefaultButton_HasTheLook_UntilACommandButtonTakesTheFocus_WhichHasItInstead()
    {
        var looks = UI.Run(() =>
        {
            var root = Form(VBClasses.TextBox, VBClasses.CommandButton, VBClasses.CommandButton);
            root.Contents[1].Set(P.Default, true);
            var window = Open(root);
            List<string> result = [Looks(window)];
            Focus(window, root.Contents[2]);
            result.Add(Looks(window));
            Focus(window, root.Contents[1]);
            result.Add(Looks(window));
            Focus(window, root.Contents[0]);
            result.Add(Looks(window));
            window.Close();
            return result;
        });

        Expect.Lines(["CommandButton2", "CommandButton3", "CommandButton2", "CommandButton2"], looks);
    }

    [TestMethod]
    public void WithNoDefaultButton_OnlyAFocusedCommandButtonHasTheLook()
    {
        var looks = UI.Run(() =>
        {
            var root = Form(VBClasses.TextBox, VBClasses.CommandButton);
            var window = Open(root);
            List<string> result = [Looks(window)];
            Focus(window, root.Contents[1]);
            result.Add(Looks(window));
            window.Close();
            return result;
        });

        Expect.Lines(["", "CommandButton2"], looks);
    }

    [TestMethod]
    public void TheLook_FollowsDefault_AndLeavesADefaultButtonThatIsHiddenOrDisabled()
    {
        var looks = UI.Run(() =>
        {
            var root = Form(VBClasses.TextBox, VBClasses.CommandButton, VBClasses.CommandButton);
            var window = Open(root);
            root.Contents[1].Set(P.Default, true);
            List<string> result = [Looks(window)];
            root.Contents[1].Set(P.Enabled, false);
            result.Add(Looks(window));
            root.Contents[1].Set(P.Enabled, true).Set(P.Visible, false);
            result.Add(Looks(window));
            root.Contents[1].Set(P.Visible, true).Set(P.Default, false);
            root.Contents[2].Set(P.Default, true);
            result.Add(Looks(window));
            window.Close();
            return result;
        });

        Expect.Lines(["CommandButton2", "", "", "CommandButton3"], looks);
    }

    [TestMethod]
    public void TheLook_IsABorderTwoPixelsWide_InTheHighlightColour_AndGoesWithIt()
    {
        var (wide, colour, after) = UI.Run(() =>
        {
            var root = Form(VBClasses.TextBox, VBClasses.CommandButton);
            root.Contents[1].Set(P.Default, true);
            var window = Open(root);
            var button = (Button)window.View.ControlOf(root.Contents[1])!;
            var result = (button.BorderThickness, (button.BorderBrush as ISolidColorBrush)?.Color, default(Thickness));
            root.Contents[1].Set(P.Default, false);
            result.Item3 = button.BorderThickness;
            window.Close();
            return result;
        });

        Assert.AreEqual(new Thickness(2), wide);
        Assert.AreEqual(Color.FromRgb(0x00, 0x78, 0xD7), colour);
        Assert.AreNotEqual(new Thickness(2), after);
    }
}

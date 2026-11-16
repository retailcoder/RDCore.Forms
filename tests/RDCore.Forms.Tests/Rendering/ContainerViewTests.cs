using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// A shown form's view following a program's Set Control.Container, as measured with VB6: the control goes on top of the windowed
// controls of its new container, even the one it was in, and the focus stays on it with no LostFocus or GotFocus.
[TestClass]
public sealed class ContainerViewTests
{
    // Frame1 at 8, 8 holding Picture1, then Command1 and Text1 on a shown form.
    private static (VBFormWindow Window, FormDefinition Form, RecordingSink Sink) Open()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
        var frame = new ComponentInstance(VBClasses.Frame, "Frame1").Set(P.TabIndex, (short)0).Set(P.Left, 8d).Set(P.Top, 8d)
            .Set(P.Width, 200d).Set(P.Height, 150d);
        frame.SetContainer(root);
        new ComponentInstance(VBClasses.PictureBox, "Picture1").Set(P.TabIndex, (short)1).Set(P.Left, 8d).Set(P.Top, 16d)
            .Set(P.Width, 100d).Set(P.Height, 100d).SetContainer(frame);
        new ComponentInstance(VBClasses.CommandButton, "Command1").Set(P.TabIndex, (short)2).Set(P.Left, 20d).Set(P.Top, 30d)
            .Set(P.Width, 80d).Set(P.Height, 30d).SetContainer(root);
        new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.TabIndex, (short)3).Set(P.Left, 8d).Set(P.Top, 180d)
            .Set(P.Width, 100d).Set(P.Height, 24d).SetContainer(root);
        var form = new FormDefinition(root);
        var window = new VBFormWindow(form);
        var sink = new RecordingSink();
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return (window, form, sink);
    }

    [TestMethod]
    public void AControlPlacedInAFrame_IsShownInIt_OnTopOfTheFramesControls_AtItsPlace()
    {
        var (inFrame, onTop, place, backOnTop) = UI.Run(() =>
        {
            var (window, form, _) = Open();
            var view = window.View;
            var button = form.Find("Command1")!;
            var frameHost = ((IVBContainer)view.ControlOf(form.Find("Frame1")!)!).ChildHost;
            form.SetContainer(button, form.Find("Frame1")!);
            Dispatcher.UIThread.RunJobs();
            var shown = view.ControlOf(button)!;
            var picture = view.ControlOf(form.Find("Picture1")!)!;
            var result = (ReferenceEquals(shown.Parent, frameHost), shown.ZIndex > picture.ZIndex, (Canvas.GetLeft(shown), Canvas.GetTop(shown)));
            window.Invoke(button, VBMethods.ZOrder, [FormValue.FromInteger(1)]);
            var sentBack = shown.ZIndex < picture.ZIndex;
            form.SetContainer(button, form.Find("Frame1")!);
            Dispatcher.UIThread.RunJobs();
            var back = sentBack && shown.ZIndex > picture.ZIndex;
            window.Close();
            return (result.Item1, result.Item2, result.Item3, back);
        });

        Assert.IsTrue(inFrame);
        Assert.IsTrue(onTop);
        Assert.AreEqual((20d, 30d), place);
        Assert.IsTrue(backOnTop, "ZOrder 1 sends it under Picture1, and a Set to the same Frame brings it back on top");
    }

    [TestMethod]
    public void TheFocusedControl_PlacedInAFrame_KeepsTheFocus_AndRaisesNothing()
    {
        var (focusedAfter, raised) = UI.Run(() =>
        {
            var (window, form, sink) = Open();
            var text = form.Find("Text1")!;
            var textControl = window.View.ControlOf(text)!;
            textControl.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            form.SetContainer(text, form.Find("Frame1")!);
            Dispatcher.UIThread.RunJobs();
            var focused = TopLevel.GetTopLevel(window.View)!.FocusManager!.GetFocusedElement() as Visual;
            var result = (focused is not null && (ReferenceEquals(focused, textControl) || textControl.IsVisualAncestorOf(focused)),
                sink.WithoutMoves.Where(e => e.Contains("Focus", StringComparison.Ordinal)).ToArray());
            window.Close();
            return result;
        });

        Assert.IsTrue(focusedAfter);
        CollectionAssert.AreEqual(Array.Empty<string>(), raised);
    }
}

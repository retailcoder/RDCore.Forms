using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// A shown form's view following Controls.Add and Controls.Remove, as measured with VB6: an added control is under those already in
// its container, and the focus leaves a control removed for the first control of the tab order.
[TestClass]
public sealed class RunTimeControlViewTests
{
    // Frame1 (TabIndex 1) at 140, 8, then Command1 (TabIndex 0) at 8, 8, on a shown form.
    private static (VBFormWindow Window, FormDefinition Form, RecordingSink Sink) Open()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
        new ComponentInstance(VBClasses.Frame, "Frame1").Set(P.TabIndex, (short)1).Set(P.Left, 140d).Set(P.Top, 8d)
            .Set(P.Width, 160d).Set(P.Height, 120d).SetContainer(root);
        new ComponentInstance(VBClasses.CommandButton, "Command1").Set(P.TabIndex, (short)0).Set(P.Left, 8d).Set(P.Top, 8d)
            .Set(P.Width, 100d).Set(P.Height, 32d).SetContainer(root);
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
    public void AControlAdded_IsShownAtOnce_InItsContainer_UnderTheControlsAlreadyThere()
    {
        var (inView, inFrame, under, hidden, tabOrder) = UI.Run(() =>
        {
            var (window, form, _) = Open();
            var button = form.AddControl("VB.CommandButton", "cmdNew");
            var inside = form.AddControl("VB.CommandButton", "cmdIn", form.Find("Frame1"));
            var view = window.View;
            var shown = view.ControlOf(button)!;
            var hidden = !shown.IsVisible;
            button.Set(P.Visible, true);
            Dispatcher.UIThread.RunJobs();
            var frameHost = ((IVBContainer)view.ControlOf(form.Find("Frame1")!)!).ChildHost;
            var result = (ReferenceEquals(shown.Parent, view), ReferenceEquals(view.ControlOf(inside)!.Parent, frameHost),
                shown.ZIndex < view.ControlOf(form.Find("Command1")!)!.ZIndex, hidden,
                view.TabOrder.Select(c => VBControl.GetInstance(c)!.Name).ToArray());
            window.Close();
            return result;
        });

        Assert.IsTrue(inView);
        Assert.IsTrue(inFrame);
        Assert.IsTrue(under);
        Assert.IsTrue(hidden);
        CollectionAssert.AreEqual(new[] { "Command1", "Frame1", "cmdNew", "cmdIn" }, tabOrder);
    }

    [TestMethod]
    public void AControlAdded_RaisesItsEvents_AsAControlOfTheFormsFileDoes()
    {
        var raised = UI.Run(() =>
        {
            var (window, form, sink) = Open();
            var button = form.AddControl("VB.CommandButton", "cmdNew");
            button.Set(P.Left, 8d).Set(P.Top, 160d).Set(P.Visible, true);
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            window.MouseDown(new Point(20, 170), MouseButton.Left);
            window.MouseUp(new Point(20, 170), MouseButton.Left);
            window.Close();
            return sink.WithoutMoves.Where(e => e.StartsWith("cmdNew.", StringComparison.Ordinal) && !e.Contains("Mouse")).ToArray();
        });

        Expect.Lines(new[] { "cmdNew.GotFocus", "cmdNew.Click" }, raised);
    }

    // VB6 snaps a ListBox added at run time to whole rows at once: 450 twips, 30 pixels at the default font. RDForms snaps it
    // as its view is made, so one added to a form not shown keeps its 32 pixels until the form is shown.
    [TestMethod]
    public void AListBoxAdded_IsSnappedToWholeRows_AsItsViewIsMade()
    {
        var (onShown, beforeShown, afterShown) = UI.Run(() =>
        {
            var (window, form, _) = Open();
            var onShown = form.AddControl("VB.ListBox", "lstNew").Get(P.Height);
            window.Close();

            var hiddenForm = new FormDefinition(new ComponentInstance(VBClasses.Form, "Form2").Set(P.Width, 320d).Set(P.Height, 240d));
            var list = hiddenForm.AddControl("VB.ListBox", "lstNew");
            var beforeShown = list.Get(P.Height);
            var hiddenWindow = new VBFormWindow(hiddenForm);
            hiddenWindow.Show();
            Dispatcher.UIThread.RunJobs();
            var afterShown = list.Get(P.Height);
            hiddenWindow.Close();
            return (onShown, beforeShown, afterShown);
        });

        Assert.AreEqual(30d, onShown);
        Assert.AreEqual(32d, beforeShown);
        Assert.AreEqual(30d, afterShown);
    }

    [TestMethod]
    public void AControlRemoved_GoesAtOnce_WithWhatItHolds_AndTheFocusGoesToTheFirstControlOfTheTabOrder()
    {
        var (gone, heldGone, focused, raised) = UI.Run(() =>
        {
            var (window, form, sink) = Open();
            var frame = form.AddControl("VB.Frame", "fraNew");
            var inFrame = form.AddControl("VB.CommandButton", "cmdInNew", frame);
            var button = form.AddControl("VB.CommandButton", "cmdNew");
            button.Set(P.Top, 160d).Set(P.Visible, true);
            Dispatcher.UIThread.RunJobs();
            var buttonControl = window.View.ControlOf(button)!;
            var heldControl = window.View.ControlOf(inFrame)!;
            buttonControl.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            form.RemoveControl(button);
            form.RemoveControl(frame);
            Dispatcher.UIThread.RunJobs();
            var focused = TopLevel.GetTopLevel(window.View)!.FocusManager!.GetFocusedElement() as Control;
            var result = (window.View.ControlOf(button) is null && TopLevel.GetTopLevel(buttonControl) is null,
                window.View.ControlOf(inFrame) is null && TopLevel.GetTopLevel(heldControl) is null,
                ReferenceEquals(focused, window.View.ControlOf(form.Find("Command1")!)), sink.Raised.ToArray());
            window.Close();
            return result;
        });

        Assert.IsTrue(gone);
        Assert.IsTrue(heldGone);
        Assert.IsTrue(focused);
        CollectionAssert.Contains(raised, "Command1.GotFocus");
    }
}

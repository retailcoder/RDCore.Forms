using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
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

[TestClass]
public sealed class FormRenderingTests
{
    private static ComponentInstance Root(ScaleMode scaleMode = ScaleMode.Twip) =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d).Set(P.ScaleMode, scaleMode);

    // A component placed in a container, in pixels.
    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double left, double top,
        double width, double height)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, left).Set(P.Top, top).Set(P.Width, width).Set(P.Height, height);
        component.SetContainer(container);
        return component;
    }

    private static (VBFormWindow Window, RecordingSink Sink) Show(ComponentInstance root)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        var sink = new RecordingSink();
        VBControl.SetEventSink(window, sink);
        window.Closing += (_, _) => VBControl.SetEventSink(window, null);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        // A hit test reads the rendered scene, which takes the stacking at a render: one is forced here, with no clock.
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        // What the form raises as it is shown is not what these tests look at.
        sink.Raised.Clear();
        return (window, sink);
    }

    // Where a control is in the view, in pixels.
    private static Rect InTheView(VBFormWindow window, Control control) =>
        new(control.TranslatePoint(default, window.View)!.Value, control.Bounds.Size);

    [TestMethod]
    public void AControlsPixels_PlaceItAsTheyAre_AndTheWindowsClientIsTheRoots()
    {
        var (bounds, client) = UI.Run(() =>
        {
            var root = Root();
            var command = Add(root, VBClasses.CommandButton, "Command1", 8, 16, 81, 33);
            var (window, _) = Show(root);
            var result = (InTheView(window, window.View.ControlOf(command)!), window.ClientSize);
            window.Close();
            return result;
        });

        Assert.AreEqual(new Rect(8, 16, 81, 33), bounds);
        Assert.AreEqual(new Size(320, 240), client);
    }

    [TestMethod]
    [DataRow(0d, 0d)]
    [DataRow(0d, 240d)]
    [DataRow(320d, 0d)]
    public void AFormOfNoWidthOrNoHeight_IsAWindowOfOnePixelAtLeast(double width, double height)
    {
        var client = UI.Run(() =>
        {
            var (window, _) = Show(new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, width).Set(P.Height, height));
            var result = window.ClientSize;
            window.Close();
            return result;
        });

        Assert.IsTrue(client.Width >= 1 && client.Height >= 1, $"The window's client is {client}.");
    }

    [TestMethod]
    public void APositionIsPixels_OnAPixelForm_InAPixelPictureBox_AndInAFrame()
    {
        var (onForm, inPicture, inFrame) = UI.Run(() =>
        {
            var root = Root(ScaleMode.Pixel);
            var text1 = Add(root, VBClasses.TextBox, "Text1", 8, 8, 81, 19);
            var picture = Add(root, VBClasses.PictureBox, "Picture1", 8, 40, 200, 100).Set(P.ScaleMode, ScaleMode.Pixel);
            var text2 = Add(picture, VBClasses.TextBox, "Text2", 10, 5, 81, 19);
            var frame = Add(root, VBClasses.Frame, "Frame1", 8, 160, 200, 60);
            var text3 = Add(frame, VBClasses.TextBox, "Text3", 16, 24, 60, 19);
            var (window, _) = Show(root);
            var view = window.View;
            var box = view.ControlOf(text3)!;
            // The Frame's controls are placed from its own top left corner, its caption's line included.
            var host = box.GetVisualParent()!.TranslatePoint(default, view.ControlOf(frame)!);
            var result = (Canvas.GetLeft(view.ControlOf(text1)!), (Canvas.GetLeft(view.ControlOf(text2)!), Canvas.GetTop(view.ControlOf(text2)!)),
                (host, Canvas.GetLeft(box), Canvas.GetTop(box)));
            window.Close();
            return result;
        });

        Assert.AreEqual(8d, onForm, 1e-9);
        Assert.AreEqual((10d, 5d), inPicture);
        Assert.AreEqual(new Point(0, 0), inFrame.host);
        Assert.AreEqual(16d, inFrame.Item2, 1e-9);
        Assert.AreEqual(24d, inFrame.Item3, 1e-9);
    }

    [TestMethod]
    public void AComponentsPositionAndSizeFromCode_MoveItsControl()
    {
        var bounds = UI.Run(() =>
        {
            var root = Root();
            var command = Add(root, VBClasses.CommandButton, "Command1", 8, 16, 81, 33);
            var (window, _) = Show(root);
            command.Set(P.Left, 20d).Set(P.Width, 100d);
            Dispatcher.UIThread.RunJobs();
            var result = InTheView(window, window.View.ControlOf(command)!);
            window.Close();
            return result;
        });

        Assert.AreEqual(new Rect(20, 16, 100, 33), bounds);
    }

    [TestMethod]
    public void TheFirstControlInAContainer_IsOnTop_AndAWindowlessOneIsUnderEveryWindowedOne()
    {
        var (overlap, label) = UI.Run(() =>
        {
            var root = Root();
            var first = Add(root, VBClasses.CommandButton, "Command1", 0, 0, 100, 33);
            Add(root, VBClasses.CommandButton, "Command2", 50, 0, 100, 33);
            var over = Add(root, VBClasses.Label, "Label1", 0, 40, 100, 33);
            var button = Add(root, VBClasses.CommandButton, "Command3", 50, 40, 100, 33);
            var (window, _) = Show(root);
            var view = window.View;
            bool Under(Point point, ComponentInstance component) =>
                window.InputHitTest(point) is Visual hit && view.ControlOf(component) is { } control
                && (ReferenceEquals(hit, control) || control.IsVisualAncestorOf(hit));
            var result = (Under(new Point(75, 10), first), Under(new Point(75, 50), button) && !Under(new Point(75, 50), over));
            window.Close();
            return result;
        });

        Assert.IsTrue(overlap);
        Assert.IsTrue(label);
    }

    [TestMethod]
    public void TheTabOrder_IsOneOrderAcrossTheForm_AndAnIndexPastTheLastTakesTheFirstFreePlace()
    {
        var order = UI.Run(() =>
        {
            var root = Root();
            Add(root, VBClasses.TextBox, "Text1", 0, 0, 80, 19).Set(P.TabIndex, (short)3);
            var frame = Add(root, VBClasses.Frame, "Frame1", 0, 40, 200, 100).Set(P.TabIndex, (short)0);
            Add(frame, VBClasses.TextBox, "Text2", 8, 16, 80, 19).Set(P.TabIndex, (short)1);
            Add(root, VBClasses.CommandButton, "Command1", 0, 160, 80, 33).Set(P.TabIndex, (short)17);
            new ComponentInstance(VBClasses.Timer, "Timer1").SetContainer(root);
            var view = new VBFormView(new FormDefinition(root));
            return view.TabOrder.Select(c => VBControl.GetInstance(c)!.Name).ToArray();
        });

        Expect.Lines(new[] { "Frame1", "Text2", "Command1", "Text1" }, order);
    }

    [TestMethod]
    public void TabAndShiftTab_MoveTheFocusThroughTheOrder_SkippingWhatCannotTakeIt()
    {
        var focused = UI.Run(() =>
        {
            var root = Root();
            var text1 = Add(root, VBClasses.TextBox, "Text1", 0, 0, 80, 19).Set(P.TabIndex, (short)0);
            var frame = Add(root, VBClasses.Frame, "Frame1", 0, 40, 200, 100).Set(P.TabIndex, (short)1);
            Add(frame, VBClasses.TextBox, "Text2", 8, 16, 80, 19).Set(P.TabIndex, (short)2);
            Add(root, VBClasses.TextBox, "Text3", 100, 0, 80, 19).Set(P.TabIndex, (short)3).Set(P.TabStop, false);
            Add(root, VBClasses.TextBox, "Text4", 200, 0, 80, 19).Set(P.TabIndex, (short)4).Set(P.Enabled, false);
            Add(root, VBClasses.TextBox, "Text5", 200, 400, 80, 19).Set(P.TabIndex, (short)5).Set(P.Visible, false);
            Add(root, VBClasses.CommandButton, "Command1", 0, 160, 80, 33).Set(P.TabIndex, (short)6);
            var (window, _) = Show(root);
            var names = new List<string>();
            void Press(RawInputModifiers modifiers)
            {
                window.KeyPress(Key.Tab, modifiers, PhysicalKey.Tab, null);
                window.KeyRelease(Key.Tab, modifiers, PhysicalKey.Tab, null);
                var at = window.FocusManager?.GetFocusedElement() as Control;
                names.Add(at is null ? "none" : VBControl.GetInstance(at)?.Name ?? at.GetType().Name);
            }
            window.View.ControlOf(text1)!.Focus();
            Press(RawInputModifiers.None);
            Press(RawInputModifiers.None);
            Press(RawInputModifiers.None);
            Press(RawInputModifiers.Shift);
            window.Close();
            return names;
        });

        Expect.Lines(new[] { "Text2", "Command1", "Text1", "Command1" }, focused);
    }

    [TestMethod]
    public void AControlInAHiddenFrame_KeepsItsVisible_IsNotShown_AndStillRaisesItsEvents()
    {
        var (visible, shown, focusable, raised) = UI.Run(() =>
        {
            var root = Root();
            var frame = Add(root, VBClasses.Frame, "Frame1", 0, 0, 200, 100).Set(P.Visible, false);
            var text = Add(frame, VBClasses.TextBox, "Text1", 8, 16, 80, 19);
            var (window, sink) = Show(root);
            var box = window.View.ControlOf(text)!;
            text.Set(P.Text, "set by code");
            var result = (text.Get(P.Visible), box.IsEffectivelyVisible, box.Focus(), sink.Raised.ToList());
            window.Close();
            return result;
        });

        Assert.IsTrue(visible);
        Assert.IsFalse(shown);
        Assert.IsFalse(focusable);
        Expect.Lines(new[] { "Text1.Change" }, raised);
    }

    [TestMethod]
    public void AControlInADisabledFrame_KeepsItsEnabled_AndTakesNoInput()
    {
        var (enabled, effectivelyEnabled, raised) = UI.Run(() =>
        {
            var root = Root();
            var frame = Add(root, VBClasses.Frame, "Frame1", 0, 0, 200, 100).Set(P.Enabled, false);
            var command = Add(frame, VBClasses.CommandButton, "Command1", 8, 16, 80, 33);
            var (window, sink) = Show(root);
            window.MouseDown(new Point(20, 25), MouseButton.Left);
            window.MouseUp(new Point(20, 25), MouseButton.Left);
            var result = (command.Get(P.Enabled), window.View.ControlOf(command)!.IsEffectivelyEnabled, sink.WithoutMoves);
            window.Close();
            return result;
        });

        Assert.IsTrue(enabled);
        Assert.IsFalse(effectivelyEnabled);
        Assert.IsFalse(raised.Any(e => e.StartsWith("Command1.", StringComparison.Ordinal)));
    }

    [TestMethod]
    public void ADisabledRoot_TakesNoInputInAnyOfItsControls()
    {
        var raised = UI.Run(() =>
        {
            var root = Root().Set(P.Enabled, false);
            Add(root, VBClasses.CommandButton, "Command1", 8, 16, 80, 33);
            var (window, sink) = Show(root);
            window.MouseDown(new Point(20, 25), MouseButton.Left);
            window.MouseUp(new Point(20, 25), MouseButton.Left);
            window.Close();
            return sink.WithoutMoves;
        });

        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void TheWindow_TakesTheRootsCaptionAndButtons_AndFollowsItsCaptionWhileOpen()
    {
        var (title, maximize, minimize, after) = UI.Run(() =>
        {
            var root = Root().Set(P.Caption, "First").Set(P.MaxButton, false);
            var (window, _) = Show(root);
            var before = (window.Title, window.CanMaximize, window.CanMinimize);
            root.Set(P.Caption, "Second");
            var result = (before.Title, before.CanMaximize, before.CanMinimize, window.Title);
            window.Close();
            return result;
        });

        Assert.AreEqual("First", title);
        Assert.IsFalse(maximize);
        Assert.IsTrue(minimize);
        Assert.AreEqual("Second", after);
    }

    [TestMethod]
    public void AWindow_IsForAForm_AndAComponentWithNoControl_IsNotShown()
    {
        var (parameter, control) = UI.Run(() =>
        {
            var error = Assert.ThrowsExactly<ArgumentException>(() => new VBFormWindow(new FormDefinition(new ComponentInstance(VBClasses.UserControl, "UserControl1"))));
            var root = Root();
            var menu = new ComponentInstance(VBClasses.Menu, "mnuFile");
            menu.SetContainer(root);
            return (error.ParamName, new VBFormView(new FormDefinition(root)).ControlOf(menu));
        });

        Assert.AreEqual("form", parameter);
        Assert.IsNull(control);
    }
}

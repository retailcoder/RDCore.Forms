using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

[TestClass]
public sealed class FormEventTests
{
    private static ComponentInstance Root(string name = "Form1", ScaleMode scaleMode = ScaleMode.Twip) =>
        new ComponentInstance(VBClasses.Form, name).Set(P.Width, 4800d).Set(P.Height, 3600d).Set(P.ScaleMode, scaleMode);

    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double left, double top,
        double width, double height)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, left).Set(P.Top, top).Set(P.Width, width).Set(P.Height, height);
        component.SetContainer(container);
        return component;
    }

    // A window with its sink, shown, with what it raised as it was shown kept.
    private static VBFormWindow Open(ComponentInstance root, RecordingSink sink)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window;
    }

    // What the window system tells a window as the user clicks its close box or drags its border. Avalonia keeps these off its public
    // API, so they are reached as the window system reaches them, on the window's platform implementation.
    private static void CloseBox(Window window)
    {
        var closing = (Func<WindowCloseReason, bool>)typeof(IWindowImpl).GetProperty("Closing")!.GetValue(window.PlatformImpl)!;
        closing(WindowCloseReason.WindowClosing);
    }

    private static void DragTheBorder(Window window, Size clientSize) =>
        typeof(IWindowImpl).GetMethod("Resize")!.Invoke(window.PlatformImpl, [clientSize, WindowResizeReason.User]);

    private static void Answer(RecordingSink sink, EventDefinition definition, Action<FormValue[]> handler) =>
        sink.Handler += (_, raised, arguments) =>
        {
            if (ReferenceEquals(raised, definition))
                handler(arguments);
        };

    [TestMethod]
    public void Shown_TheFormRaisesResizeThenActivate_ThenPaint()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            var result = sink.Raised.ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize", "Form1.Activate", "Form1.Paint"], raised);
    }

    [TestMethod]
    public void ASizeSetFromCode_RaisesOneResize_PaintAsItGrows_AndNoneForTheSameSize()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            root.Set(P.Width, 6000d);
            Dispatcher.UIThread.RunJobs();
            root.Set(P.Width, 6000d);
            root.Set(P.Height, 3000d);
            Dispatcher.UIThread.RunJobs();
            var result = (sink.Raised.ToList(), window.View.Bounds.Width);
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize", "Form1.Paint", "Form1.Resize"], raised.Item1);
        Assert.AreEqual(400, raised.Width);
    }

    [TestMethod]
    public void TheUserResizingTheWindow_SetsTheRootsSize_AndRaisesOneResize()
    {
        var (raised, width, height) = UI.Run(() =>
        {
            var root = Root();
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            DragTheBorder(window, new Size(400, 300));
            Dispatcher.UIThread.RunJobs();
            var result = (sink.Raised.ToList(), root.Get(P.Width), root.Get(P.Height));
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize", "Form1.Paint"], raised);
        Assert.AreEqual(6000d, width);
        Assert.AreEqual(4500d, height);
    }

    [TestMethod]
    public void AFormShownAgainAfterHide_RaisesNoResize()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            sink.Raised.Clear();
            window.Hide();
            window.Show();
            Dispatcher.UIThread.RunJobs();
            var result = sink.Raised.ToList();
            window.Close();
            return result;
        });

        Assert.DoesNotContain("Form1.Resize", raised);
    }

    [TestMethod]
    public void AnotherFormActivated_RaisesDeactivateOnTheActiveOne_ThenActivate()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var first = Open(Root("Form1"), sink);
            var second = Open(Root("Form2"), sink);
            first.Activate();
            Dispatcher.UIThread.RunJobs();
            var result = sink.Raised.Where(e => !e.EndsWith(".Paint", StringComparison.Ordinal)).ToList();
            second.Close();
            first.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize", "Form1.Activate",
            "Form2.Resize", "Form1.Deactivate", "Form2.Activate",
            "Form2.Deactivate", "Form1.Activate"], raised);
    }

    [TestMethod]
    public void AnotherApplicationTakingTheActivationAndGivingItBack_RaisesNothing()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var form = Open(Root(), sink);
            sink.Raised.Clear();
            var other = new Window();
            other.Show();
            Dispatcher.UIThread.RunJobs();
            form.Activate();
            Dispatcher.UIThread.RunJobs();
            var result = sink.Raised.ToList();
            other.Close();
            form.Close();
            return result;
        });

        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void AFormWhoseRootIsDisabled_IsShownWithoutBeingActivated()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root().Set(P.Enabled, false), sink);
            window.Activate();
            Dispatcher.UIThread.RunJobs();
            var result = sink.Raised.ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize", "Form1.Paint"], raised);
    }

    [TestMethod]
    public void TheCloseBox_RaisesQueryUnloadWithMode0_ThenUnload_AndCloses()
    {
        var (raised, open) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            sink.Raised.Clear();
            CloseBox(window);
            Dispatcher.UIThread.RunJobs();
            return (sink.Raised.ToList(), window.IsVisible);
        });

        Expect.Lines(["Form1.QueryUnload(Integer 0, Integer 0)", "Form1.Unload(Integer 0)"], raised);
        Assert.IsFalse(open);
    }

    [TestMethod]
    [DataRow("QueryUnload")]
    [DataRow("Unload")]
    public void ACancelSet_KeepsTheWindowOpen(string cancelledIn)
    {
        var (raised, open) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            sink.Raised.Clear();
            Answer(sink, cancelledIn == "Unload" ? VBEvents.Unload : VBEvents.QueryUnload, arguments => arguments[0] = FormValue.FromInteger(-1));
            CloseBox(window);
            Dispatcher.UIThread.RunJobs();
            var result = (sink.Raised.ToList(), window.IsVisible);
            window.Close();
            return result;
        });

        Expect.Lines(cancelledIn == "Unload"
            ? ["Form1.QueryUnload(Integer 0, Integer 0)", "Form1.Unload(Integer 0)"]
            : ["Form1.QueryUnload(Integer 0, Integer 0)"], raised);
        Assert.IsTrue(open);
    }

    [TestMethod]
    public void Unload_RaisesQueryUnloadWithMode1_ThenUnload_AndAnUnloadInsideThemDoesNothing()
    {
        var (raised, unloaded, inner, open) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            sink.Raised.Clear();
            bool? nested = null;
            Answer(sink, VBEvents.QueryUnload, _ => nested = window.Unload());
            var result = window.Unload();
            Dispatcher.UIThread.RunJobs();
            return (sink.Raised.ToList(), result, nested, window.IsVisible);
        });

        Expect.Lines(["Form1.QueryUnload(Integer 0, Integer 1)", "Form1.Unload(Integer 0)"], raised);
        Assert.IsTrue(unloaded);
        Assert.IsFalse(inner);
        Assert.IsFalse(open);
    }

    [TestMethod]
    public void CloseFromCode_RaisesNeitherQueryUnloadNorUnload()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            sink.Raised.Clear();
            window.Close();
            return sink.Raised.ToList();
        });

        Assert.IsEmpty(raised);
    }

    [TestMethod]
    [DataRow(ScaleMode.Twip, "Single 4500, Single 150")]
    [DataRow(ScaleMode.Pixel, "Single 300, Single 10")]
    public void APressOnNoControl_IsTheForms_InItsScale_WithClickAfterMouseUp(ScaleMode scaleMode, string at)
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(scaleMode: scaleMode), sink);
            sink.Raised.Clear();
            window.MouseDown(new Point(300, 10), MouseButton.Left);
            window.MouseUp(new Point(300, 10), MouseButton.Left);
            var result = sink.WithoutMoves;
            window.Close();
            return result;
        });

        Expect.Lines([$"Form1.MouseDown(Integer 1, Integer 0, {at})", $"Form1.MouseUp(Integer 1, Integer 0, {at})", "Form1.Click"], raised);
    }

    [TestMethod]
    [DataRow(MouseButton.Right, 2)]
    [DataRow(MouseButton.Middle, 4)]
    public void TheForm_RaisesClick_ForEveryButton(MouseButton button, int vbButton)
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            sink.Raised.Clear();
            window.MouseDown(new Point(300, 10), button);
            window.MouseUp(new Point(250, 150), button);
            var result = sink.WithoutMoves;
            window.Close();
            return result;
        });

        Expect.Lines([$"Form1.MouseDown(Integer {vbButton}, Integer 0, Single 4500, Single 150)",
            $"Form1.MouseUp(Integer {vbButton}, Integer 0, Single 3750, Single 2250)", "Form1.Click"], raised);
    }

    [TestMethod]
    public void AReleaseOverAControl_GivesTheFormItsMouseUp_AndNoClick()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            Add(root, VBClasses.Label, "Label1", 150, 1200, 1200, 255);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            window.MouseDown(new Point(300, 10), MouseButton.Left);
            window.MouseUp(new Point(21, 83), MouseButton.Left);
            var result = sink.WithoutMoves;
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.MouseDown(Integer 1, Integer 0, Single 4500, Single 150)",
            "Form1.MouseUp(Integer 1, Integer 0, Single 315, Single 1245)"], raised);
    }

    [TestMethod]
    public void AKeyWithNoControlFocused_IsTheForms()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Root(), sink);
            sink.Raised.Clear();
            window.KeyPress(Key.K, RawInputModifiers.None, PhysicalKey.K, "k");
            window.KeyTextInput("k");
            window.KeyRelease(Key.K, RawInputModifiers.None, PhysicalKey.K, "k");
            window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null);
            var result = sink.Raised.ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.KeyDown(Integer 75, Integer 0)", "Form1.KeyPress(Integer 107)", "Form1.KeyUp(Integer 75, Integer 0)",
            "Form1.KeyDown(Integer 8, Integer 0)", "Form1.KeyPress(Integer 8)"], raised);
    }

    // The KeyPreview cases measured with VB6 for a TextBox: the form's KeyDown sets 88 (x) to 0, its KeyPress sets 122 (z) to 0 and
    // 101 (e) to 69, and the box's KeyDown sets 75 (k) to 0.
    [TestMethod]
    [DataRow(false, "k", "k", "D75,P107,C,U75")]
    [DataRow(true, "k", "k", "FD75,D75,FP107,P107,C,FU75,U75")]
    [DataRow(true, "x", "x", "FD88,FP120,P120,C,FU88,U88")]
    [DataRow(true, "z", "", "FD90,D90,FP122,FU90,U90")]
    [DataRow(true, "e", "E", "FD69,D69,FP101,P69,C,FU69,U69")]
    public void WithKeyPreview_TheFormRaisesEachKeyEventBeforeTheTextBox(bool preview, string typed, string text, string events)
    {
        var (raised, after) = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, preview);
            var text1 = Add(root, VBClasses.TextBox, "Text1", 120, 120, 1800, 315);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            var box = (VBTextBox)window.View.ControlOf(text1)!;
            box.Focus();
            Dispatcher.UIThread.RunJobs();
            var log = new List<string>();
            sink.Handler = (source, definition, arguments) =>
            {
                var form = ReferenceEquals(source, root);
                var code = arguments.Length > 0 ? arguments[0].AsInteger() : (short)0;
                var tag = definition.Name switch { "KeyDown" => "D", "KeyPress" => "P", "KeyUp" => "U", "Change" => "C", _ => null };
                if (tag is null)
                    return;
                log.Add((form ? "F" : "") + tag + (tag == "C" ? "" : code.ToString()));
                if (form && tag == "D" && code == 88 || form && tag == "P" && code == 122 || !form && tag == "D" && code == 75)
                    arguments[0] = FormValue.FromInteger(0);
                if (form && tag == "P" && code == 101)
                    arguments[0] = FormValue.FromInteger(69);
            };
            var key = Key.A + (typed[0] - 'a');
            var physical = PhysicalKey.A + (typed[0] - 'a');
            window.KeyPress(key, RawInputModifiers.None, physical, typed);
            window.KeyTextInput(typed);
            window.KeyRelease(key, RawInputModifiers.None, physical, typed);
            var result = (string.Join(",", log), box.Text ?? "");
            window.Close();
            return result;
        });

        Assert.AreEqual(events, raised);
        Assert.AreEqual(text, after);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void AKeyOnAControlThatRaisesNoKeyEvents_IsTheFormsOnlyWithKeyPreview(bool preview)
    {
        var raised = UI.Run(() =>
        {
            var root = Root().Set(P.KeyPreview, preview);
            var command1 = Add(root, VBClasses.CommandButton, "Command1", 120, 120, 1200, 495);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            window.View.ControlOf(command1)!.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            window.KeyPress(Key.K, RawInputModifiers.None, PhysicalKey.K, "k");
            window.KeyTextInput("k");
            window.KeyRelease(Key.K, RawInputModifiers.None, PhysicalKey.K, "k");
            var result = sink.Raised.ToList();
            window.Close();
            return result;
        });

        Expect.Lines(preview
            ? ["Form1.KeyDown(Integer 75, Integer 0)", "Form1.KeyPress(Integer 107)", "Form1.KeyUp(Integer 75, Integer 0)"]
            : [], raised);
    }
}

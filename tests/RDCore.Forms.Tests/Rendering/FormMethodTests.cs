using Avalonia.Headless;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using M = RDCore.Forms.Model.VB.VBMethods;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// The methods a program's code calls on a form and its controls. Move, SetFocus and ZOrder as measured with VB6, native and p-code
// agreeing; Refresh as RDForms sets it.
[TestClass]
public sealed class FormMethodTests
{
    // A form holding a control of each class named, one under the other, each named for its class and its place (TextBox1, Label2).
    private static ComponentInstance Form(string name, params ComponentClass[] classes)
    {
        var root = new ComponentInstance(VBClasses.Form, name).Set(P.Width, 320d).Set(P.Height, 240d);
        var top = 8d;
        foreach (var (type, index) in classes.Select((type, index) => (type, index)))
        {
            var control = new ComponentInstance(type, $"{type.ClassName}{index + 1}").Set(P.Left, 8d).Set(P.Top, top)
                .Set(P.Width, 120d).Set(P.Height, 21d);
            control.SetContainer(root);
            top += 33;
        }
        return root;
    }

    private static VBFormWindow Window(ComponentInstance root, RecordingSink sink)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        return window;
    }

    private static VBFormWindow Open(ComponentInstance root, RecordingSink sink)
    {
        var window = Window(root, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window;
    }

    private static List<string> Raised(RecordingSink sink) => [.. sink.Raised.Where(e => !e.EndsWith(".Paint", StringComparison.Ordinal))];

    private static FormValue S(double value) => FormValue.FromSingle((float)value);

    // The error a call answers with, or 0.
    private static int ErrorOf(Action call)
    {
        try
        {
            call();
            return 0;
        }
        catch (FormErrorException e)
        {
            return e.Error.Number;
        }
    }

    // In twips.
    private static string Extent(ComponentInstance component)
    {
        static double Twips(double pixels) => Math.Round(pixels * 15, 6);
        return $"{Twips(component.Get(P.Left))}, {Twips(component.Get(P.Top))}, {Twips(component.Get(P.Width))} x {Twips(component.Get(P.Height))}";
    }

    [TestMethod]
    public void AControlsMove_InVB6Units_IsInItsContainersScale_AndSetsOnlyWhatItIsGiven()
    {
        var extents = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox);
            var text = root.Contents[0];
            var window = Open(root, new RecordingSink());
            List<string> result = [];
            window.Invoke(text, M.Move, [S(600)], CodeUnit.VB6);
            result.Add(Extent(text));
            window.Invoke(text, M.Move, [S(600), S(900), S(1200), S(450)], CodeUnit.VB6);
            result.Add(Extent(text));
            root.Set(P.ScaleMode, ScaleMode.Pixel);
            window.Invoke(text, M.Move, [S(100), S(10)], CodeUnit.VB6);
            result.Add(Extent(text));
            window.Close();
            return result;
        });

        Expect.Lines(["600, 120, 1800 x 315", "600, 900, 1200 x 450", "1500, 150, 1200 x 450"], extents);
    }

    [TestMethod]
    public void Move_InVB6Units_RoundsToAWholeTwip_HalfToEven()
    {
        var extents = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox);
            var text = root.Contents[0];
            var window = Open(root, new RecordingSink());
            List<string> result = [];
            window.Invoke(text, M.Move, [S(600.5), S(900.5), S(4800.5), S(3600.5)], CodeUnit.VB6);
            result.Add(Extent(text));
            window.Invoke(text, M.Move, [S(607.5), S(907.5)], CodeUnit.VB6);
            result.Add(Extent(text));
            window.Close();
            return result;
        });

        Expect.Lines(["600, 900, 4800 x 3600", "608, 908, 4800 x 3600"], extents);
    }

    [TestMethod]
    public void Move_ChecksEveryValueBeforeItMovesAnything()
    {
        var (errors, extent) = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox);
            var text = root.Contents[0];
            var window = Open(root, new RecordingSink());
            var move = (FormValue?[] arguments) => ErrorOf(() => window.Invoke(text, M.Move, arguments, CodeUnit.VB6));
            int[] result =
            [
                move([S(600), S(900), S(-10), S(3600)]),
                move([S(600), S(900), S(4800), S(-10)]),
                move([S(3E+9)]),
                move([S(600), S(900), S(3E+9)]),
                move([FormValue.Null]),
                move([S(600), FormValue.Null]),
                move([S(600), FormValue.FromString("x")]),
                move([S(600), null, S(4800)]),
                move([]),
                move([S(1), S(2), S(3), S(4), S(5)]),
            ];
            var after = Extent(text);
            window.Close();
            return (result, after);
        });

        CollectionAssert.AreEqual(new[] { 5, 5, 6, 6, 94, 94, 13, 448, 449, 450 }, errors);
        Assert.AreEqual("120, 120, 1800 x 315", extent);
    }

    [TestMethod]
    public void Move_TakesAStringThatIsANumber()
    {
        var extent = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox);
            var text = root.Contents[0];
            var window = Open(root, new RecordingSink());
            window.Invoke(text, M.Move, [S(600), FormValue.FromString("900")], CodeUnit.VB6);
            var result = Extent(text);
            window.Close();
            return result;
        });

        Assert.AreEqual("600, 900, 1800 x 315", extent);
    }

    [TestMethod]
    public void AFormsMove_RaisesOneResize_ForBothSizes_AndNoneForTheSizeItHas()
    {
        var raised = UI.Run(() =>
        {
            var root = Form("Form1");
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            window.Invoke(root, M.Move, [S(10), S(20), S(400), S(300)]);
            Dispatcher.UIThread.RunJobs();
            var once = Raised(sink);
            sink.Raised.Clear();
            window.Invoke(root, M.Move, [S(10), S(20), S(400), S(300)]);
            Dispatcher.UIThread.RunJobs();
            var result = (once, again: Raised(sink), size: (root.Get(P.Width), root.Get(P.Height)));
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize"], raised.once);
        Expect.Lines([], raised.again);
        Assert.AreEqual((400d, 300d), raised.size);
    }

    [TestMethod]
    public void AFormsMove_InVB6Units_IsInTwips_WhateverItsScaleMode()
    {
        var left = UI.Run(() =>
        {
            var root = Form("Form1");
            root.Set(P.ScaleMode, ScaleMode.Pixel);
            var window = Open(root, new RecordingSink());
            window.Invoke(root, M.Move, [S(150)], CodeUnit.VB6);
            var result = root.Get(P.Left);
            window.Close();
            return result;
        });

        Assert.AreEqual(10d, left);
    }

    [TestMethod]
    public void SetFocus_BeforeTheFormIsShown_Is5_OnAControl_AndOnTheForm()
    {
        var errors = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox);
            var window = Window(root, new RecordingSink());
            int[] result = [ErrorOf(() => window.Invoke(root.Contents[0], M.SetFocus, [])), ErrorOf(() => window.Invoke(root, M.SetFocus, []))];
            window.Close();
            return result;
        });

        CollectionAssert.AreEqual(new[] { 5, 5 }, errors);
    }

    [TestMethod]
    public void SetFocus_OnADisabledOrAHiddenControl_Is5_AndChangesNothing()
    {
        var (errors, active, raised) = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox, VBClasses.TextBox, VBClasses.TextBox);
            root.Contents[1].Set(P.Enabled, false);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            root.Contents[2].Set(P.Visible, false);
            int[] result = [ErrorOf(() => window.Invoke(root.Contents[1], M.SetFocus, [])), ErrorOf(() => window.Invoke(root.Contents[2], M.SetFocus, []))];
            Dispatcher.UIThread.RunJobs();
            var after = (result, window.ActiveControl?.Name, Raised(sink));
            window.Close();
            return after;
        });

        CollectionAssert.AreEqual(new[] { 5, 5 }, errors);
        Assert.AreEqual("TextBox1", active);
        Expect.Lines([], raised);
    }

    [TestMethod]
    public void SetFocus_NamesTheControlAtOnce_AndMovesTheFocusAfterTheCall_LostFocusFirst()
    {
        var (active, during, after) = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox, VBClasses.TextBox);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            window.Invoke(root.Contents[1], M.SetFocus, []);
            var result = (window.ActiveControl?.Name, Raised(sink), new List<string>());
            Dispatcher.UIThread.RunJobs();
            result.Item3.AddRange(Raised(sink));
            window.Close();
            return result;
        });

        Assert.AreEqual("TextBox2", active);
        Expect.Lines([], during);
        Expect.Lines(["TextBox1.LostFocus", "TextBox2.GotFocus"], after);
    }

    [TestMethod]
    public void SetFocus_OnTheFocusedControl_AndOnTheActiveForm_RaisesNothing()
    {
        var raised = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            window.Invoke(root.Contents[0], M.SetFocus, []);
            window.Invoke(root, M.SetFocus, []);
            Dispatcher.UIThread.RunJobs();
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines([], raised);
    }

    [TestMethod]
    public void SetFocus_OnAControlOfAnotherForm_ActivatesThatForm_AndTheControlTakesTheFocus()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var root = Form("Form1", VBClasses.TextBox, VBClasses.TextBox);
            var first = Open(root, sink);
            var second = Open(Form("Form2", VBClasses.CheckBox), sink);
            sink.Raised.Clear();
            first.Invoke(root.Contents[1], M.SetFocus, []);
            Dispatcher.UIThread.RunJobs();
            var result = Raised(sink);
            second.Close();
            first.Close();
            return result;
        });

        Expect.Lines(["CheckBox1.LostFocus", "Form2.Deactivate", "Form1.Activate", "TextBox2.GotFocus"], raised);
    }

    [TestMethod]
    public void SetFocus_OnAnOptionButton_SelectsIt_AndItsClickFollowsItsGotFocus()
    {
        var (raised, value) = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox, VBClasses.OptionButton);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            sink.Raised.Clear();
            window.Invoke(root.Contents[1], M.SetFocus, []);
            Dispatcher.UIThread.RunJobs();
            var result = (Raised(sink), root.Contents[1].Get(P.OptionButtonValue));
            window.Close();
            return result;
        });

        Expect.Lines(["TextBox1.LostFocus", "OptionButton2.GotFocus", "OptionButton2.Click"], raised);
        Assert.IsTrue(value);
    }

    [TestMethod]
    public void ZOrder_TakesAPositionOf0Or1_AsAnIntegerRoundedHalfToEven()
    {
        var outcomes = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.CommandButton, VBClasses.CommandButton);
            var window = Open(root, new RecordingSink());
            var first = window.View.ControlOf(root.Contents[0])!;
            var second = window.View.ControlOf(root.Contents[1])!;
            // Where the second button goes: to the front, the back, or nowhere for an error.
            string ZOrder(FormValue? position)
            {
                var error = ErrorOf(() => window.Invoke(root.Contents[1], M.ZOrder, position is { } given ? [given] : []));
                var outcome = error != 0 ? $"{error}" : second.ZIndex > first.ZIndex ? "front" : "back";
                window.Invoke(root.Contents[1], M.ZOrder, [FormValue.FromInteger(1)]);
                return outcome;
            }
            List<string> result =
            [
                ZOrder(null), ZOrder(FormValue.FromInteger(0)), ZOrder(FormValue.Empty), ZOrder(FormValue.FromDouble(-0.5)),
                ZOrder(FormValue.FromDouble(0.6)), ZOrder(FormValue.FromString("1")), ZOrder(FormValue.FromInteger(2)),
                ZOrder(FormValue.FromInteger(-1)), ZOrder(FormValue.FromDouble(1.5)), ZOrder(FormValue.FromString("x")),
                ZOrder(FormValue.FromLong(32768)), ZOrder(FormValue.Null),
            ];
            window.Close();
            return result;
        });

        Expect.Lines(["front", "front", "front", "front", "back", "back", "5", "5", "5", "13", "6", "13"], outcomes);
    }

    [TestMethod]
    public void ZOrder_KeepsAControlInItsPlane_AWindowlessOneUnderEveryWindowedOne()
    {
        var order = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.CommandButton, VBClasses.Label, VBClasses.Label);
            var window = Open(root, new RecordingSink());
            window.Invoke(root.Contents[2], M.ZOrder, []);
            window.Invoke(root.Contents[0], M.ZOrder, [FormValue.FromInteger(1)]);
            // Top first.
            var result = string.Join(", ", root.Contents.OrderByDescending(c => window.View.ControlOf(c)!.ZIndex).Select(c => c.Name));
            window.Close();
            return result;
        });

        Assert.AreEqual("CommandButton1, Label3, Label2", order);
    }

    [TestMethod]
    public void Refresh_OnTheForm_RaisesPaintInsideTheCall_WhereAutoRedrawIsFalse()
    {
        var raised = UI.Run(() =>
        {
            var root = Form("Form1");
            var sink = new RecordingSink();
            var window = Open(root, sink);
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            window.Invoke(root, M.Refresh, []);
            var during = sink.Raised.ToList();
            root.Set(P.AutoRedraw, true);
            sink.Raised.Clear();
            window.Invoke(root, M.Refresh, []);
            var result = (during, redrawn: sink.Raised.ToList());
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Paint"], raised.during);
        Expect.Lines([], raised.redrawn);
    }

    [TestMethod]
    public void Invoke_RefusesAMethodTheClassHasNot_AndAComponentTheFormDoesNotShow()
    {
        UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.Label);
            var window = Open(root, new RecordingSink());
            Assert.ThrowsExactly<ArgumentException>(() => window.Invoke(root.Contents[0], M.SetFocus, []));
            Assert.ThrowsExactly<ArgumentException>(() => window.Invoke(new ComponentInstance(VBClasses.TextBox, "Text9"), M.Refresh, []));
            window.Close();
        });
    }
}

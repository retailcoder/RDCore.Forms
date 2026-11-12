using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// MultiLine, ScrollBars, PasswordChar and MaxLength. The MultiLine and ScrollBars rows were measured with VB6, native and p-code
// agreeing; the others are RDForms' own.
[TestClass]
public sealed class TextBoxPropertyTests
{
    // Text1, the box under test, then Text2, and Command1 where the form has a default button.
    private static (ComponentInstance Root, ComponentInstance Box) Form(bool multiLine, bool defaultButton = false)
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
        var box = Add(root, VBClasses.TextBox, "Text1", 8).Set(P.MultiLine, multiLine).Set(P.Height, 60d);
        Add(root, VBClasses.TextBox, "Text2", 96);
        if (defaultButton)
            Add(root, VBClasses.CommandButton, "Command1", 160).Set(P.Default, true);
        return (root, box);
    }

    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double top)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, 8d).Set(P.Top, top).Set(P.Width, 160d).Set(P.Height, 33d);
        component.SetContainer(container);
        return component;
    }

    // The window, with the box focused and what was raised so far forgotten.
    private static VBFormWindow Open(ComponentInstance root, ComponentInstance box, RecordingSink sink)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.View.ControlOf(box)!.Focus();
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return window;
    }

    // A key pressed and released. The window system types no character for Enter or Tab.
    private static void Press(VBFormWindow window, Key key, PhysicalKey physical, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
        window.KeyRelease(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static void Type(VBFormWindow window, string text)
    {
        window.KeyTextInput(text);
        Dispatcher.UIThread.RunJobs();
    }

    [TestMethod]
    public void EnterAndCtrlEnter_BreakAMultiLineBoxsLine_WithCrLf_AfterTheirKeyPress()
    {
        var (text, raised) = UI.Run(() =>
        {
            var (root, box) = Form(multiLine: true);
            var sink = new RecordingSink();
            var window = Open(root, box, sink);
            Type(window, "x");
            Press(window, Key.Enter, PhysicalKey.Enter);
            Type(window, "y");
            Press(window, Key.Enter, PhysicalKey.Enter, RawInputModifiers.Control);
            Type(window, "z");
            window.Close();
            return (box.Get(P.Text), sink.WithoutMoves);
        });

        Assert.AreEqual("x\r\ny\r\nz", text);
        Expect.Lines(new[]
        {
            "Text1.KeyPress(Integer 120)", "Text1.Change",
            "Text1.KeyDown(Integer 13, Integer 0)", "Text1.KeyPress(Integer 13)", "Text1.Change", "Text1.KeyUp(Integer 13, Integer 0)",
            "Text1.KeyPress(Integer 121)", "Text1.Change",
            "Text1.KeyDown(Integer 13, Integer 2)", "Text1.KeyPress(Integer 10)", "Text1.Change", "Text1.KeyUp(Integer 13, Integer 2)",
            "Text1.KeyPress(Integer 122)", "Text1.Change",
        }, raised);
    }

    [TestMethod]
    public void EnterInASingleLineBox_TypesNothing()
    {
        var text = UI.Run(() =>
        {
            var (root, box) = Form(multiLine: false);
            var window = Open(root, box, new RecordingSink());
            Type(window, "x");
            Press(window, Key.Enter, PhysicalKey.Enter);
            Type(window, "y");
            window.Close();
            return box.Get(P.Text);
        });

        Assert.AreEqual("xy", text);
    }

    [TestMethod]
    public void AKeyAsciiOfZero_ForEnter_BreaksNoLine()
    {
        var text = UI.Run(() =>
        {
            var (root, box) = Form(multiLine: true);
            var sink = new RecordingSink
            {
                Handler = (_, definition, arguments) =>
                {
                    if (ReferenceEquals(definition, VBEvents.KeyPress) && arguments[0].AsInteger() == 13)
                        arguments[0] = FormValue.FromInteger(0);
                },
            };
            var window = Open(root, box, sink);
            Type(window, "x");
            Press(window, Key.Enter, PhysicalKey.Enter);
            Type(window, "y");
            window.Close();
            return box.Get(P.Text);
        });

        Assert.AreEqual("xy", text);
    }

    [TestMethod]
    public void TabInAMultiLineBox_MovesTheFocus_AndTypesNothing()
    {
        var (text, other) = UI.Run(() =>
        {
            var (root, box) = Form(multiLine: true);
            var next = Add(root, VBClasses.TextBox, "Text3", 200);
            var window = Open(root, box, new RecordingSink());
            Type(window, "x");
            Press(window, Key.Tab, PhysicalKey.Tab);
            Press(window, Key.Tab, PhysicalKey.Tab);
            Type(window, "y");
            window.Close();
            return (box.Get(P.Text), next.Get(P.Text));
        });

        Assert.AreEqual("x", text);
        Assert.AreEqual("y", other);
    }

    [TestMethod]
    public void EnterInAMultiLineBox_ClicksTheDefaultButton_AndBreaksNoLine()
    {
        var (text, raised) = UI.Run(() =>
        {
            var (root, box) = Form(multiLine: true, defaultButton: true);
            var sink = new RecordingSink();
            var window = Open(root, box, sink);
            Type(window, "x");
            Press(window, Key.Enter, PhysicalKey.Enter);
            Type(window, "y");
            window.Close();
            return (box.Get(P.Text), sink.WithoutMoves);
        });

        Assert.AreEqual("xy", text);
        CollectionAssert.Contains(raised, "Command1.Click");
        CollectionAssert.DoesNotContain(raised, "Text1.KeyPress(Integer 13)");
    }

    [TestMethod]
    [DataRow(false, TextBoxScrollBars.Both, TextWrapping.NoWrap, ScrollBarVisibility.Hidden, ScrollBarVisibility.Disabled)]
    [DataRow(true, TextBoxScrollBars.None, TextWrapping.Wrap, ScrollBarVisibility.Disabled, ScrollBarVisibility.Hidden)]
    [DataRow(true, TextBoxScrollBars.Horizontal, TextWrapping.NoWrap, ScrollBarVisibility.Visible, ScrollBarVisibility.Hidden)]
    [DataRow(true, TextBoxScrollBars.Vertical, TextWrapping.Wrap, ScrollBarVisibility.Disabled, ScrollBarVisibility.Visible)]
    [DataRow(true, TextBoxScrollBars.Both, TextWrapping.NoWrap, ScrollBarVisibility.Visible, ScrollBarVisibility.Visible)]
    public void AMultiLineBox_WrapsUnlessItHasAHorizontalBar_AndShowsTheBarsItAsksFor_WhereASingleLineBoxShowsNone(
        bool multiLine, TextBoxScrollBars bars, TextWrapping wrapping, ScrollBarVisibility horizontal, ScrollBarVisibility vertical)
    {
        var shown = UI.Run(() =>
        {
            var (root, box) = Form(multiLine);
            box.Set(P.ScrollBars, bars);
            var window = Open(root, box, new RecordingSink());
            var control = (VBTextBox)window.View.ControlOf(box)!;
            var viewer = control.GetVisualDescendants().OfType<ScrollViewer>().Single();
            var result = (control.TextWrapping, viewer.HorizontalScrollBarVisibility, viewer.VerticalScrollBarVisibility);
            window.Close();
            return result;
        });

        Assert.AreEqual((wrapping, horizontal, vertical), shown);
    }

    [TestMethod]
    public void AMultiLineBox_KeepsItsMultiLineAndScrollBars_AsItWasMade()
    {
        var shown = UI.Run(() =>
        {
            var box = new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.MultiLine, true);
            var control = new VBTextBox(box);
            box.Set(P.MultiLine, false).Set(P.ScrollBars, TextBoxScrollBars.Horizontal);
            return (control.AcceptsReturn, control.TextWrapping);
        });

        Assert.AreEqual((true, TextWrapping.Wrap), shown);
    }

    [TestMethod]
    public void PasswordChar_ShowsItsFirstCharacter_InASingleLineBox_AndNothingInAMultiLineOne()
    {
        var shown = UI.Run(() =>
        {
            var single = new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.PasswordChar, "*");
            var control = new VBTextBox(single);
            var multi = new VBTextBox(new ComponentInstance(VBClasses.TextBox, "Text2").Set(P.MultiLine, true).Set(P.PasswordChar, "*"));
            var (window, _) = ControlHost.Show((control, new Rect(10, 5, 120, 30)), (multi, new Rect(10, 45, 120, 60)));
            var star = control.PasswordChar;
            single.Set(P.PasswordChar, "#$");
            var first = control.PasswordChar;
            single.Set(P.PasswordChar, "");
            var none = control.PasswordChar;
            window.Close();
            return (star, first, none, multi.PasswordChar);
        });

        Assert.AreEqual(('*', '#', '\0', '\0'), shown);
    }

    [TestMethod]
    public void MaxLength_LimitsWhatTheUserTypes_WithAKeyPressForEachKey_AndAChangeForEachCharacterKept()
    {
        var (text, raised) = UI.Run(() =>
        {
            var (root, box) = Form(multiLine: false);
            box.Set(P.MaxLength, 2);
            var sink = new RecordingSink();
            var window = Open(root, box, sink);
            Type(window, "abc");
            window.Close();
            return (box.Get(P.Text), sink.WithoutMoves);
        });

        Assert.AreEqual("ab", text);
        Expect.Lines(new[]
        {
            "Text1.KeyPress(Integer 97)", "Text1.Change",
            "Text1.KeyPress(Integer 98)", "Text1.Change",
            "Text1.KeyPress(Integer 99)",
        }, raised);
    }

    [TestMethod]
    public void MaxLength_KeepsATextSetFromCodeWhole_AndZeroOrLessSetsNoLimit()
    {
        var (fromCode, typed) = UI.Run(() =>
        {
            var (root, box) = Form(multiLine: false);
            box.Set(P.MaxLength, 2);
            var window = Open(root, box, new RecordingSink());
            box.Set(P.Text, "abcdef");
            Dispatcher.UIThread.RunJobs();
            var fromCode = ((VBTextBox)window.View.ControlOf(box)!).Text;
            box.Set(P.Text, "").Set(P.MaxLength, -1);
            Type(window, "abc");
            window.Close();
            return (fromCode, box.Get(P.Text));
        });

        Assert.AreEqual("abcdef", fromCode);
        Assert.AreEqual("abc", typed);
    }
}

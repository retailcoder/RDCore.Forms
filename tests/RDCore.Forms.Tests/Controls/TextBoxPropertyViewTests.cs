using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using ScrollBars = RDCore.Forms.Model.VB.ScrollBars;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A TextBox's MultiLine, ScrollBars, PasswordChar and MaxLength in the box, as measured with VB6, native and p-code agreeing.
[TestClass]
public sealed class TextBoxPropertyViewTests
{
    // Enter, or Ctrl+Enter, pressed and released. The window system types no character for either.
    private static void Enter(Window window, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(Key.Enter, modifiers, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
        window.KeyRelease(Key.Enter, modifiers, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static List<string> Typed(RecordingSink sink) =>
        [.. sink.Raised.Where(line => line.Contains(".KeyPress(") || line.EndsWith(".Change"))];

    private static (TextWrapping, bool, ScrollBarVisibility, ScrollBarVisibility) Lines(TextBox box) =>
        (box.TextWrapping, box.AcceptsReturn, ScrollViewer.GetHorizontalScrollBarVisibility(box), ScrollViewer.GetVerticalScrollBarVisibility(box));

    [TestMethod]
    [DataRow(false, ScrollBars.Both, TextWrapping.NoWrap, false, ScrollBarVisibility.Hidden, ScrollBarVisibility.Hidden)]
    [DataRow(true, ScrollBars.None, TextWrapping.Wrap, true, ScrollBarVisibility.Disabled, ScrollBarVisibility.Hidden)]
    [DataRow(true, ScrollBars.Horizontal, TextWrapping.NoWrap, true, ScrollBarVisibility.Visible, ScrollBarVisibility.Hidden)]
    [DataRow(true, ScrollBars.Vertical, TextWrapping.Wrap, true, ScrollBarVisibility.Disabled, ScrollBarVisibility.Visible)]
    [DataRow(true, ScrollBars.Both, TextWrapping.NoWrap, true, ScrollBarVisibility.Visible, ScrollBarVisibility.Visible)]
    public void ABoxWrapsItsLines_UnlessItIsSingleLineOrScrollsAcross_AndShowsItsScrollBarsOnlyWhenMultiLine(
        bool multiLine, ScrollBars bars, TextWrapping wrapping, bool acceptsReturn, ScrollBarVisibility across, ScrollBarVisibility down)
    {
        var (made, changed) = UI.Run(() =>
        {
            var made = new VBTextBox(new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.MultiLine, multiLine).Set(P.ScrollBars, bars));
            var instance = new ComponentInstance(VBClasses.TextBox, "Text2");
            var changed = new VBTextBox(instance);
            var (window, _) = ControlHost.Show(changed);
            instance.Set(P.MultiLine, multiLine).Set(P.ScrollBars, bars);
            var lines = (Lines(made), Lines(changed));
            window.Close();
            return lines;
        });

        Assert.AreEqual((wrapping, acceptsReturn, across, down), made);
        Assert.AreEqual((wrapping, acceptsReturn, across, down), changed);
    }

    [TestMethod]
    public void ASingleLineBox_TypesNothingForEnter_AndAMultiLineBoxTypesCRLF_ForEnterAndCtrlEnter()
    {
        var (single, singleRaised, multi, multiRaised) = UI.Run(() =>
        {
            var singleInstance = new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.Text, "ab");
            var singleBox = new VBTextBox(singleInstance);
            var (window, sink) = ControlHost.Show(singleBox);
            singleBox.Focus();
            singleBox.CaretIndex = 1;
            Enter(window);
            window.Close();

            var multiInstance = new ComponentInstance(VBClasses.TextBox, "Text2").Set(P.MultiLine, true).Set(P.Text, "ab");
            var multiBox = new VBTextBox(multiInstance);
            var (multiWindow, multiSink) = ControlHost.Show(multiBox);
            multiBox.Focus();
            multiBox.CaretIndex = 1;
            Enter(multiWindow);
            Enter(multiWindow, RawInputModifiers.Control);
            multiWindow.Close();
            return (singleInstance.Get(P.Text), Typed(sink), multiInstance.Get(P.Text), Typed(multiSink));
        });

        Assert.AreEqual("ab", single);
        Expect.Lines(new[] { "Text1.KeyPress(Integer 13)" }, singleRaised);
        Assert.AreEqual("a\r\n\r\nb", multi);
        Expect.Lines(new[] { "Text2.KeyPress(Integer 13)", "Text2.Change", "Text2.KeyPress(Integer 10)", "Text2.Change" }, multiRaised);
    }

    [TestMethod]
    public void AKeyPressHandler_CancelsAMultiLineBoxsEnterAndCtrlEnterWithZero_OrTypesAnotherCharacter()
    {
        var text = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.MultiLine, true);
            var box = new VBTextBox(instance);
            var (window, sink) = ControlHost.Show(box);
            var codes = new Queue<short>([0, 0, 65]);
            sink.Handler = (_, definition, arguments) =>
            {
                if (ReferenceEquals(definition, VBEvents.KeyPress))
                    arguments[0] = FormValue.FromInteger(codes.Dequeue());
            };
            box.Focus();
            Enter(window);
            Enter(window, RawInputModifiers.Control);
            Enter(window);
            window.Close();
            return instance.Get(P.Text);
        });

        Assert.AreEqual("A", text);
    }

    [TestMethod]
    public void ACharacterTypedInAFullBox_IsAKeyPress_AndChangesNothing()
    {
        var (text, shown, raised) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.MaxLength, 3).Set(P.Text, "abc");
            var box = new VBTextBox(instance);
            var (window, sink) = ControlHost.Show(box);
            box.Focus();
            box.CaretIndex = 3;
            window.KeyTextInput("x");
            window.Close();
            return (instance.Get(P.Text), box.Text, Typed(sink));
        });

        Assert.AreEqual("abc", text);
        Assert.AreEqual("abc", shown);
        Expect.Lines(new[] { "Text1.KeyPress(Integer 120)" }, raised);
    }

    [TestMethod]
    public void ATextLongerThanALowerMaxLength_IsShownWhole_AndTypingStopsAtTheLimit()
    {
        var (shown, held, limit) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.Text, "abcdef").Set(P.MaxLength, 3);
            var box = new VBTextBox(instance);
            var (window, _) = ControlHost.Show(box);
            var shown = box.Text;
            window.Close();
            return (shown, instance.Get(P.Text), box.MaxLength);
        });

        Assert.AreEqual("abcdef", shown);
        Assert.AreEqual("abcdef", held);
        Assert.AreEqual(3, limit);
    }

    [TestMethod]
    public void APasswordChar_MasksTheBox_AndTheTextIsWhatWasTyped()
    {
        var (masked, text, cleared) = UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.TextBox, "Text1").Set(P.PasswordChar, "*");
            var box = new VBTextBox(instance);
            var (window, _) = ControlHost.Show(box);
            box.Focus();
            window.KeyTextInput("ab");
            var masked = box.PasswordChar;
            instance.Set(P.PasswordChar, "");
            window.Close();
            return (masked, instance.Get(P.Text), box.PasswordChar);
        });

        Assert.AreEqual('*', masked);
        Assert.AreEqual("ab", text);
        Assert.AreEqual('\0', cleared);
    }
}

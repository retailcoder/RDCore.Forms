using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// Enter and Esc, with and without a Default and a Cancel button, as measured with VB6, native and p-code agreeing.
[TestClass]
public sealed class DefaultCancelTests
{
    private static ComponentInstance Root() =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);

    private static ComponentInstance Add(ComponentInstance container, ComponentClass type, string name, double top)
    {
        var component = new ComponentInstance(type, name).Set(P.Left, 8d).Set(P.Top, top).Set(P.Width, 120d).Set(P.Height, 33d);
        component.SetContainer(container);
        return component;
    }

    // A form holding the control under test, then Command1, the default button, and Command2, the cancel button, where it has them.
    private static (ComponentInstance Root, ComponentInstance Control) Form(string type, bool buttons)
    {
        var root = Root();
        var control = Add(root, VBClasses.ByTypeName("VB." + type)!, type == "CommandButton" ? "Command3" : "Control1", 8);
        if (type == "OptionButton")
            control.Set(P.OptionButtonValue, true);
        Add(root, VBClasses.CommandButton, "Command1", 96).Set(P.Default, buttons);
        Add(root, VBClasses.CommandButton, "Command2", 160).Set(P.Cancel, buttons);
        return (root, control);
    }

    // The window, with the control focused, where there is one, and what was raised so far forgotten.
    private static VBFormWindow Open(ComponentInstance root, ComponentInstance? focused, RecordingSink sink)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        if (focused is not null)
            window.View.ControlOf(focused)!.Focus();
        Dispatcher.UIThread.RunJobs();
        sink.Raised.Clear();
        return window;
    }

    // A key pressed and released. The window system types no character for Enter or Esc.
    private static void Press(VBFormWindow window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        var physical = key == Key.Enter ? PhysicalKey.Enter : PhysicalKey.Escape;
        window.KeyPress(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
        window.KeyRelease(key, modifiers, physical, null);
        Dispatcher.UIThread.RunJobs();
    }

    private static List<string> Raised(RecordingSink sink) => [.. sink.WithoutMoves];

    [TestMethod]
    [DataRow("TextBox", false)]
    [DataRow("TextBox", true)]
    [DataRow("ListBox", false)]
    [DataRow("CheckBox", false)]
    [DataRow("CheckBox", true)]
    [DataRow("OptionButton", false)]
    public void EnterAndEsc_ClickTheDefaultAndTheCancelButton_WithNoKeyEvent_NotEvenTheFormsUnderKeyPreview(string type, bool preview)
    {
        var raised = UI.Run(() =>
        {
            var (root, control) = Form(type, buttons: true);
            root.Set(P.KeyPreview, preview);
            var sink = new RecordingSink();
            var window = Open(root, control, sink);
            Press(window, Key.Enter);
            Press(window, Key.Escape);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Command1.Click", "Command2.Click"], raised);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void EnterOnAFocusedCommandButton_ClicksIt_WithNoKeyEvent_WhetherOrNotTheFormHasADefaultButton(bool buttons)
    {
        var raised = UI.Run(() =>
        {
            var (root, command3) = Form("CommandButton", buttons);
            var sink = new RecordingSink();
            var window = Open(root, command3, sink);
            Press(window, Key.Enter);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Command3.Click"], raised);
    }

    // Not measured: RDForms' own, the rule of a single-line box kept for a multi-line one.
    [TestMethod]
    public void EnterInAMultiLineBox_ClicksTheDefaultButton_AndCtrlEnterTypesANewLine()
    {
        var (raised, text) = UI.Run(() =>
        {
            var (root, control) = Form("TextBox", buttons: true);
            control.Set(P.MultiLine, true);
            var sink = new RecordingSink();
            var window = Open(root, control, sink);
            Press(window, Key.Enter);
            Press(window, Key.Enter, RawInputModifiers.Control);
            var result = Raised(sink).Where(line => !line.Contains(".Key")).ToList();
            window.Close();
            return (result, control.Get(P.Text));
        });

        Expect.Lines(["Command1.Click", "Control1.Change"], raised);
        Assert.AreEqual("\r\n", text);
    }

    [TestMethod]
    public void EscOnACommandButton_ClicksTheCancelButton()
    {
        var raised = UI.Run(() =>
        {
            var (root, command3) = Form("CommandButton", buttons: true);
            var sink = new RecordingSink();
            var window = Open(root, command3, sink);
            Press(window, Key.Escape);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Command2.Click"], raised);
    }

    [TestMethod]
    [DataRow("TextBox")]
    [DataRow("ListBox")]
    [DataRow("CheckBox")]
    [DataRow("OptionButton")]
    public void WithNeitherButton_EnterAndEsc_RaiseKeyDownKeyPressAndKeyUp_OnTheControl_AndClickNothing(string type)
    {
        var (raised, value) = UI.Run(() =>
        {
            var (root, control) = Form(type, buttons: false);
            var sink = new RecordingSink();
            var window = Open(root, control, sink);
            Press(window, Key.Enter);
            Press(window, Key.Escape);
            var result = (Raised(sink), type == "CheckBox" ? control.Get(P.CheckBoxValue) : CheckBoxValue.Unchecked);
            window.Close();
            return result;
        });

        Expect.Lines(["Control1.KeyDown(Integer 13, Integer 0)", "Control1.KeyPress(Integer 13)", "Control1.KeyUp(Integer 13, Integer 0)",
            "Control1.KeyDown(Integer 27, Integer 0)", "Control1.KeyPress(Integer 27)", "Control1.KeyUp(Integer 27, Integer 0)"], raised);
        Assert.AreEqual(CheckBoxValue.Unchecked, value);
    }

    [TestMethod]
    public void WithNoCancelButton_EscOnACommandButton_RaisesItsKeyEvents()
    {
        var raised = UI.Run(() =>
        {
            var (root, command3) = Form("CommandButton", buttons: false);
            var sink = new RecordingSink();
            var window = Open(root, command3, sink);
            Press(window, Key.Escape);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Command3.KeyDown(Integer 27, Integer 0)", "Command3.KeyPress(Integer 27)", "Command3.KeyUp(Integer 27, Integer 0)"], raised);
    }

    [TestMethod]
    public void CtrlEnter_IsAKey_WithKeyAscii10_AndClicksNoDefaultButton()
    {
        var raised = UI.Run(() =>
        {
            var (root, text) = Form("TextBox", buttons: true);
            var sink = new RecordingSink();
            var window = Open(root, text, sink);
            Press(window, Key.Enter, RawInputModifiers.Control);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Control1.KeyDown(Integer 13, Integer 2)", "Control1.KeyPress(Integer 10)", "Control1.KeyUp(Integer 13, Integer 2)"], raised);
    }

    [TestMethod]
    [DataRow(Key.Enter, true)]
    [DataRow(Key.Enter, false)]
    [DataRow(Key.Escape, true)]
    [DataRow(Key.Escape, false)]
    public void EnterOrEscInAnOpenList_ClosesIt_ClicksNoButton_AndRaisesItsKeyUpOnlyWhereTheFormHasNoButtonForIt(Key key, bool buttons)
    {
        var (raised, open) = UI.Run(() =>
        {
            var root = Root();
            var combo1 = Add(root, VBClasses.ComboBox, "Combo1", 8).Set(P.Height, 21d).Set(P.ComboBoxStyle, ComboBoxStyle.DropdownList)
                .Set(P.List, ["alpha", "beta"]);
            Add(root, VBClasses.CommandButton, "Command1", 96).Set(P.Default, buttons);
            Add(root, VBClasses.CommandButton, "Command2", 160).Set(P.Cancel, buttons);
            var sink = new RecordingSink();
            var window = Open(root, combo1, sink);
            var combo = (VBComboBox)window.View.ControlOf(combo1)!;
            combo.ListIndex = 0;
            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            Press(window, key);
            var result = (Raised(sink), combo.IsDropDownOpen);
            window.Close();
            return result;
        });

        var code = key == Key.Enter ? 13 : 27;
        List<string> expected = [$"Combo1.KeyDown(Integer {code}, Integer 0)", $"Combo1.KeyPress(Integer {code})"];
        if (!buttons)
            expected.Add($"Combo1.KeyUp(Integer {code}, Integer 0)");
        Expect.Lines(expected, raised);
        Assert.IsFalse(open);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ADefaultButtonHiddenOrDisabled_TakesNoKey(bool hidden)
    {
        var raised = UI.Run(() =>
        {
            var (root, text) = Form("TextBox", buttons: true);
            var command1 = root.Contents.Single(component => component.Name == "Command1");
            command1.Set(hidden ? P.Visible : P.Enabled, false);
            var sink = new RecordingSink();
            var window = Open(root, text, sink);
            Press(window, Key.Enter);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Control1.KeyDown(Integer 13, Integer 0)", "Control1.KeyPress(Integer 13)", "Control1.KeyUp(Integer 13, Integer 0)"], raised);
    }

    [TestMethod]
    public void OfTwoDefaultButtons_TheFirstTheFormHolds_TakesEnter()
    {
        var raised = UI.Run(() =>
        {
            var (root, text) = Form("TextBox", buttons: true);
            root.Contents.Single(component => component.Name == "Command2").Set(P.Default, true);
            var sink = new RecordingSink();
            var window = Open(root, text, sink);
            Press(window, Key.Enter);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Command1.Click"], raised);
    }

    [TestMethod]
    public void OnAFormWithNoControls_EnterAndEsc_RaiseTheFormsKeyDownKeyPressAndKeyUp()
    {
        var raised = UI.Run(() =>
        {
            var root = Root();
            var sink = new RecordingSink();
            var window = Open(root, null, sink);
            Press(window, Key.Enter);
            Press(window, Key.Escape);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.KeyDown(Integer 13, Integer 0)", "Form1.KeyPress(Integer 13)", "Form1.KeyUp(Integer 13, Integer 0)",
            "Form1.KeyDown(Integer 27, Integer 0)", "Form1.KeyPress(Integer 27)", "Form1.KeyUp(Integer 27, Integer 0)"], raised);
    }
}

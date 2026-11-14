using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Xaml;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Controls;

// A CommandButton's Value set from code, as measured with VB6, native and p-code agreeing.
[TestClass]
public sealed class CommandButtonValueTests
{
    // What was raised, with the Value each Click read, and the Value and whether it is set once the statement is done.
    private static (List<string> Raised, bool Value, bool IsSet, bool Focused) SetValue(bool value, Action<ComponentInstance>? arrange = null,
        bool nested = false)
    {
        return UI.Run(() =>
        {
            var instance = new ComponentInstance(VBClasses.CommandButton, "Command1");
            arrange?.Invoke(instance);
            var button = new VBCommandButton(instance);
            var (window, sink) = ControlHost.Show(button);
            sink.Raised.Clear();
            var clicks = 0;
            sink.Handler = (source, definition, _) =>
            {
                if (definition.Name != "Click")
                    return;
                sink.Raised.Add($"Value={source.Get(P.CommandButtonValue)}");
                if (nested && ++clicks == 1)
                    source.Set(P.CommandButtonValue, true);
            };
            instance.Set(P.CommandButtonValue, value);
            var result = (sink.WithoutMoves, instance.Get(P.CommandButtonValue), instance.IsSet(P.CommandButtonValue), button.IsFocused);
            window.Close();
            return result;
        });
    }

    [TestMethod]
    public void ValueTrue_RaisesClick_ThatReadsTrue_AndIsFalseAgainAfter_WithNoFocusOrMouseEvent()
    {
        var (raised, value, isSet, focused) = SetValue(true);

        Expect.Lines(["Command1.Click", "Value=True"], raised);
        Assert.IsFalse(value);
        Assert.IsFalse(isSet);
        Assert.IsFalse(focused);
    }

    [TestMethod]
    public void ValueFalse_RaisesNothing()
    {
        var (raised, value, isSet, _) = SetValue(false);

        Assert.IsEmpty(raised);
        Assert.IsFalse(value);
        Assert.IsTrue(isSet);
    }

    [TestMethod]
    [DataRow("Enabled")]
    [DataRow("Visible")]
    public void ValueTrue_ClicksAButtonThatIsDisabledOrHidden(string property)
    {
        var (raised, value, _, _) = SetValue(true, instance => instance.Set(property == "Enabled" ? P.Enabled : P.Visible, false));

        Expect.Lines(["Command1.Click", "Value=True"], raised);
        Assert.IsFalse(value);
    }

    [TestMethod]
    public void ValueTrue_SetInClick_RaisesANestedClick()
    {
        var (raised, value, isSet, _) = SetValue(true, nested: true);

        Expect.Lines(["Command1.Click", "Value=True", "Command1.Click", "Value=True"], raised);
        Assert.IsFalse(value);
        Assert.IsFalse(isSet);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ValueTrue_OnAFormsWindowNeverShown_ClicksTheButton_EnabledOrNot(bool formEnabled)
    {
        var raised = UI.Run(() =>
        {
            var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d).Set(P.Enabled, formEnabled);
            var button = new ComponentInstance(VBClasses.CommandButton, "Command1").Set(P.Width, 120d).Set(P.Height, 33d);
            button.SetContainer(root);
            var window = new VBFormWindow(new FormDefinition(root));
            var sink = new RecordingSink();
            VBControl.SetEventSink(window, sink);
            Dispatcher.UIThread.RunJobs();
            button.Set(P.CommandButtonValue, true);
            return (sink.Raised, button.Get(P.CommandButtonValue));
        });

        Expect.Lines(["Command1.Click"], raised.Raised);
        Assert.IsFalse(raised.Item2);
    }

    [TestMethod]
    public void AButtonClickedFromCode_IsWrittenWithNoValue()
    {
        var written = UI.Run(() =>
        {
            var root = new ComponentInstance(VBClasses.Form, "Form1");
            var button = new ComponentInstance(VBClasses.CommandButton, "Command1");
            button.SetContainer(root);
            var form = new FormDefinition(root);
            _ = new VBFormWindow(form);
            button.Set(P.CommandButtonValue, true);
            return FormXaml.Write(form);
        });

        Assert.DoesNotContain("Value", written);
    }
}

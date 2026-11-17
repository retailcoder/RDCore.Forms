using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Tests.Controls;
using M = RDCore.Forms.Model.Methods;
using P = RDCore.Forms.Model.Properties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// The events the focus raises as it leaves a control of the active form for another: a TextBox's BeforeUpdate and AfterUpdate where
// the user edited it, then its Validate where the other control's CausesValidation is True, then its LostFocus and the other's
// GotFocus. A handler that cancels the BeforeUpdate or the Validate keeps the focus on the control, and nothing else is raised.
[TestClass]
public sealed class FormValidationTests
{
    // A form holding a control of each class named, one under the other, each named for its class and its place (TextBox1, CommandButton2).
    private static ComponentInstance Form(params ComponentClass[] classes)
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d);
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

    private static RDFormsFormWindow Open(ComponentInstance root, RecordingSink sink)
    {
        var window = new RDFormsFormWindow(new FormDefinition(root));
        RDFormsControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window;
    }

    private static Control ControlOf(RDFormsFormWindow window, int place) => window.View.ControlOf(window.Root.Contents[place])!;

    // The focus moved to a control, as the user moves it, and the events it raised.
    private static List<string> MoveTo(RDFormsFormWindow window, RecordingSink sink, int place)
    {
        sink.Raised.Clear();
        ControlOf(window, place).Focus();
        Dispatcher.UIThread.RunJobs();
        return [.. sink.Raised.Where(e => !e.EndsWith(".Paint", StringComparison.Ordinal))];
    }

    // The name of the component whose control has the window's focus.
    private static string? Focused(RDFormsFormWindow window) =>
        (window.FocusManager?.GetFocusedElement() as Visual)?.GetSelfAndVisualAncestors().OfType<Control>()
            .Select(RDFormsControl.GetInstance).FirstOrDefault(instance => instance is not null)?.Name;

    private static void Cancel(RecordingSink sink, string eventName) =>
        sink.Handler = (_, definition, arguments) =>
        {
            if (definition.Name == eventName)
                arguments[0] = FormValue.FromBoolean(true);
        };

    [TestMethod]
    public void TheFocusMovedToAControlThatCausesValidation_RaisesValidate_ThenLostFocus_ThenGotFocus()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form(ComponentClasses.TextBox, ComponentClasses.TextBox), sink);
            var result = MoveTo(window, sink, 1);
            window.Close();
            return result;
        });

        Expect.Lines(["TextBox1.Validate(Boolean False)", "TextBox1.LostFocus", "TextBox2.GotFocus"], raised);
    }

    [TestMethod]
    public void TheFocusMovedToAControlWhoseCausesValidationIsFalse_RaisesNoValidate()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var root = Form(ComponentClasses.TextBox, ComponentClasses.CommandButton);
            root.Contents[1].Set(P.CausesValidation, false);
            var window = Open(root, sink);
            var result = MoveTo(window, sink, 1);
            window.Close();
            return result;
        });

        Expect.Lines(["TextBox1.LostFocus", "CommandButton2.GotFocus"], raised);
    }

    [TestMethod]
    public void AValidateThatCancels_KeepsTheFocusOnTheControl_AndRaisesNothingElse_ThenTheNextMoveRaisesItAgain()
    {
        var (cancelled, focused, next) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form(ComponentClasses.TextBox, ComponentClasses.CheckBox), sink);
            Cancel(sink, "Validate");
            var first = MoveTo(window, sink, 1);
            var stays = Focused(window);
            sink.Handler = null;
            var second = MoveTo(window, sink, 1);
            window.Close();
            return (first, stays, second);
        });

        Expect.Lines(["TextBox1.Validate(Boolean False)"], cancelled);
        Assert.AreEqual("TextBox1", focused);
        Expect.Lines(["TextBox1.Validate(Boolean False)", "TextBox1.LostFocus", "CheckBox2.GotFocus"], next);
    }

    [TestMethod]
    public void ATextBoxTheUserEdited_RaisesBeforeUpdateAndAfterUpdate_BeforeItsValidate_OnceForTheEdit()
    {
        var (edited, again) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form(ComponentClasses.TextBox, ComponentClasses.TextBox), sink);
            ((TextBox)ControlOf(window, 0)).Text = "typed";
            var first = MoveTo(window, sink, 1);
            MoveTo(window, sink, 0);
            var second = MoveTo(window, sink, 1);
            window.Close();
            return (first, second);
        });

        Expect.Lines(["TextBox1.BeforeUpdate(Boolean False)", "TextBox1.AfterUpdate", "TextBox1.Validate(Boolean False)", "TextBox1.LostFocus",
            "TextBox2.GotFocus"], edited);
        Expect.Lines(["TextBox1.Validate(Boolean False)", "TextBox1.LostFocus", "TextBox2.GotFocus"], again);
    }

    [TestMethod]
    public void ATextBoxTheProgramChanged_RaisesNoBeforeUpdate()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form(ComponentClasses.TextBox, ComponentClasses.TextBox), sink);
            window.Root.Contents[0].Set(P.Text, "from code");
            Dispatcher.UIThread.RunJobs();
            var result = MoveTo(window, sink, 1);
            window.Close();
            return result;
        });

        Expect.Lines(["TextBox1.Validate(Boolean False)", "TextBox1.LostFocus", "TextBox2.GotFocus"], raised);
    }

    [TestMethod]
    public void ABeforeUpdateThatCancels_KeepsTheFocusAndTheEdit_AndTheNextMoveRaisesItAgain()
    {
        var (cancelled, focused, text, next) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form(ComponentClasses.TextBox, ComponentClasses.TextBox), sink);
            ((TextBox)ControlOf(window, 0)).Text = "typed";
            Cancel(sink, "BeforeUpdate");
            var first = MoveTo(window, sink, 1);
            var stays = Focused(window);
            var kept = window.Root.Contents[0].Get(P.Text);
            sink.Handler = null;
            var second = MoveTo(window, sink, 1);
            window.Close();
            return (first, stays, kept, second);
        });

        Expect.Lines(["TextBox1.BeforeUpdate(Boolean False)"], cancelled);
        Assert.AreEqual("TextBox1", focused);
        Assert.AreEqual("typed", text);
        Expect.Lines(["TextBox1.BeforeUpdate(Boolean False)", "TextBox1.AfterUpdate", "TextBox1.Validate(Boolean False)", "TextBox1.LostFocus",
            "TextBox2.GotFocus"], next);
    }

    [TestMethod]
    public void SetFocus_RaisesNoValidate()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form(ComponentClasses.TextBox, ComponentClasses.TextBox), sink);
            sink.Raised.Clear();
            window.Invoke(window.Root.Contents[1], M.SetFocus, []);
            Dispatcher.UIThread.RunJobs();
            var result = sink.Raised.Where(e => !e.EndsWith(".Paint", StringComparison.Ordinal)).ToList();
            window.Close();
            return result;
        });

        Expect.Lines(["TextBox1.LostFocus", "TextBox2.GotFocus"], raised);
    }

    // A click on a CommandButton moves the focus to it: a Validate that cancels keeps the focus where it was, and the button raises no Click.
    [TestMethod]
    public void AValidateThatCancels_AsTheUserClicksAButton_KeepsTheFocus_AndTheButtonRaisesNoClick()
    {
        var (raised, focused) = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form(ComponentClasses.TextBox, ComponentClasses.CommandButton), sink);
            sink.Raised.Clear();
            Cancel(sink, "Validate");
            var button = ControlOf(window, 1);
            var at = button.TranslatePoint(new Point(10, 10), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            window.MouseUp(at, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();
            var result = sink.WithoutMoves.Where(e => !e.EndsWith(".Paint", StringComparison.Ordinal)).ToList();
            var stays = Focused(window);
            window.Close();
            return (result, stays);
        });

        Assert.IsFalse(raised.Contains("CommandButton2.Click"), string.Join(" | ", raised));
        Assert.Contains("TextBox1.Validate(Boolean False)", raised);
        Assert.AreEqual("TextBox1", focused);
    }
}

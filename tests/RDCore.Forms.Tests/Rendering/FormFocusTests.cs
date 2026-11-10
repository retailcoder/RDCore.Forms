using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Tests.Controls;
using P = RDCore.Forms.Model.VB.VBProperties;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Rendering;

// The focus as the program's active form changes, as measured with VB6, native and p-code agreeing: the old form's control's
// LostFocus, the old form's Deactivate, the new form's Activate, then the new form's control's GotFocus. A form keeps the control
// that last had the focus, and gives it the focus again as it is activated again.
[TestClass]
public sealed class FormFocusTests
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

    private static VBFormWindow Open(ComponentInstance root, RecordingSink sink)
    {
        var window = new VBFormWindow(new FormDefinition(root));
        VBControl.SetEventSink(window, sink);
        window.Show();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        return window;
    }

    private static List<string> Raised(RecordingSink sink) => [.. sink.Raised.Where(e => !e.EndsWith(".Paint", StringComparison.Ordinal))];

    [TestMethod]
    public void Shown_TheFirstControlOfTheTabOrder_TakesTheFocus_AfterActivate()
    {
        var raised = UI.Run(() =>
        {
            var root = Form("Form1", VBClasses.TextBox, VBClasses.TextBox);
            // The second box is first in the tab order.
            root.Contents[1].Set(P.TabIndex, (short)0);
            root.Contents[0].Set(P.TabIndex, (short)1);
            var sink = new RecordingSink();
            var window = Open(root, sink);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize", "Form1.Activate", "TextBox2.GotFocus"], raised);
    }

    [TestMethod]
    public void AFormWhoseControlsCannotTakeTheFocus_RaisesItsOwnGotFocus_AfterActivate()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form("Form1", VBClasses.Label, VBClasses.Frame), sink);
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["Form1.Resize", "Form1.Activate", "Form1.GotFocus"], raised);
    }

    [TestMethod]
    public void AnotherFormShown_ThenTheFirstActivatedAgain_EachFormsControlLosesTheFocusBeforeItsDeactivate_AndTheControlThatHadItTakesItBack()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var first = Open(Form("Form1", VBClasses.TextBox, VBClasses.TextBox), sink);
            first.View.ControlOf(first.Root.Contents[1])!.Focus();
            Dispatcher.UIThread.RunJobs();
            sink.Raised.Clear();
            var second = Open(Form("Form2", VBClasses.TextBox), sink);
            first.Activate();
            Dispatcher.UIThread.RunJobs();
            var result = Raised(sink);
            second.Close();
            first.Close();
            return result;
        });

        Expect.Lines(["Form2.Resize", "TextBox2.LostFocus", "Form1.Deactivate", "Form2.Activate", "TextBox1.GotFocus",
            "TextBox1.LostFocus", "Form2.Deactivate", "Form1.Activate", "TextBox2.GotFocus"], raised);
    }

    [TestMethod]
    public void AnotherApplicationTakingTheActivationAndGivingItBack_RaisesNoFocusEvent()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var form = Open(Form("Form1", VBClasses.TextBox), sink);
            sink.Raised.Clear();
            var other = new Window();
            other.Show();
            Dispatcher.UIThread.RunJobs();
            form.Activate();
            Dispatcher.UIThread.RunJobs();
            var result = Raised(sink);
            other.Close();
            form.Close();
            return result;
        });

        Assert.IsEmpty(raised);
    }

    [TestMethod]
    public void TheFocusMovedBetweenTheActiveFormsControls_RaisesLostFocusThenGotFocus()
    {
        var raised = UI.Run(() =>
        {
            var sink = new RecordingSink();
            var window = Open(Form("Form1", VBClasses.TextBox, VBClasses.TextBox), sink);
            sink.Raised.Clear();
            window.View.ControlOf(window.Root.Contents[1])!.Focus();
            Dispatcher.UIThread.RunJobs();
            var result = Raised(sink);
            window.Close();
            return result;
        });

        Expect.Lines(["TextBox1.LostFocus", "TextBox2.GotFocus"], raised);
    }
}

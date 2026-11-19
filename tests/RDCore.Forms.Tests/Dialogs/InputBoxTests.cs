using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using RDCore.Forms.Avalonia.Dialogs;
using UI = RDCore.Forms.Tests.Headless.Headless;

namespace RDCore.Forms.Tests.Dialogs;

/// <summary>
/// The dialog of an <c>InputBox</c> in an RDForms application: the text it starts with, how it is confirmed and cancelled, its <em>Help</em>, and where it
/// is placed.
/// </summary>
[TestClass]
public sealed class InputBoxTests
{
    // The dialog shown on its own, what the test does to it, and what it closed with.
    private static (string? Answer, bool Closed) Answer(Action<RDFormsInputBox> act, string defaultText = "", bool hasHelp = false)
        => UI.Run(() =>
        {
            var dialog = new RDFormsInputBox("Your name?", "Project1", defaultText, hasHelp: hasHelp);
            var shown = dialog.ShowAsync();
            Dispatcher.UIThread.RunJobs();
            act(dialog);
            Dispatcher.UIThread.RunJobs();
            var closed = shown.IsCompleted;
            if (!closed)
                dialog.Cancel();

            Dispatcher.UIThread.RunJobs();
            return (closed ? UI.ResultOf(shown) : null, closed);
        });

    // A key that closes the dialog is released on no window.
    private static void Press(Window window, Key key, PhysicalKey physical)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, null);
        if (window.IsVisible)
            window.KeyRelease(key, RawInputModifiers.None, physical, null);
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    [TestMethod]
    public void TheTextBox_StartsWithTheDefault_Selected_AndHasTheFocus()
    {
        var (text, selected, focused) = UI.Run(() =>
        {
            var dialog = new RDFormsInputBox("Your name?", "Project1", "Jane");
            dialog.Show();
            Dispatcher.UIThread.RunJobs();
            var state = (dialog.Input.Text, dialog.Input.SelectedText, dialog.Input.IsFocused);
            dialog.Close();
            return state;
        });

        Assert.AreEqual("Jane", text);
        Assert.AreEqual("Jane", selected);
        Assert.IsTrue(focused);
    }

    [TestMethod]
    public void EnterAndOK_AnswerWhatWasTyped()
    {
        Assert.AreEqual("Ada", Answer(dialog =>
        {
            dialog.KeyTextInput("Ada");
            Press(dialog, Key.Enter, PhysicalKey.Enter);
        }).Answer, "Enter");
        Assert.AreEqual("Jane", Answer(dialog => Click(dialog.OkButton), "Jane").Answer, "OK");
    }

    [TestMethod]
    public void OK_OnAnEmptyTextBox_AnswersAZeroLengthString()
    {
        var (answer, closed) = Answer(dialog => Click(dialog.OkButton));

        Assert.IsTrue(closed);
        Assert.AreEqual(string.Empty, answer);
    }

    [TestMethod]
    public void EscCancelAndTheCloseButton_AnswerNothing()
    {
        foreach (var (how, act) in new (string, Action<RDFormsInputBox>)[]
                 {
                     ("Esc", dialog => Press(dialog, Key.Escape, PhysicalKey.Escape)),
                     ("Cancel", dialog => Click(dialog.CancelButton)),
                     ("the close button", dialog => dialog.Close()),
                 })
        {
            var (answer, closed) = Answer(act, "Jane");
            Assert.IsTrue(closed, how);
            Assert.IsNull(answer, how);
        }
    }

    [TestMethod]
    public void AHelpTopic_GivesAHelpButton_WhichAsksForHelp_AndTheDialogStaysOpen()
    {
        var asked = 0;
        var (_, closed) = Answer(dialog =>
        {
            dialog.HelpRequested += (_, _) => asked++;
            Click(dialog.HelpButton!);
            Press(dialog, Key.F1, PhysicalKey.F1);
        }, hasHelp: true);

        Assert.AreEqual(2, asked);
        Assert.IsFalse(closed);
        Assert.IsNull(UI.Run(() => new RDFormsInputBox("Prompt", "Title").HelpButton), "no Help button with no help topic");
    }

    [TestMethod]
    [DataRow(null, null, 1.0, 400, 300, DisplayName = "centered across, a third of the way down")]
    [DataRow(1500, 3000, 1.0, 100, 200, DisplayName = "at the twips it is given")]
    [DataRow(1500, 3000, 1.5, 150, 300, DisplayName = "in the screen's pixels")]
    [DataRow(1500, null, 1.0, 100, 300, DisplayName = "one edge given, the other placed")]
    public void TheDialog_IsPlacedAsInputBoxPlacesIt(int? xPos, int? yPos, double scaling, int x, int y)
    {
        var screen = new PixelRect(0, 0, 1200, 1050);
        var dialog = new PixelSize(400, 150);

        Assert.AreEqual(new PixelPoint(x, y), RDFormsInputBox.PositionOf(xPos, yPos, screen, scaling, dialog));
    }

    [TestMethod]
    public void TheDialog_IsPlacedOnItsScreen_WhereverTheScreenIs()
        => Assert.AreEqual(new PixelPoint(1920 + 100, 200),
            RDFormsInputBox.PositionOf(1500, 3000, new PixelRect(1920, 0, 1920, 1080), 1.0, new PixelSize(400, 150)));
}

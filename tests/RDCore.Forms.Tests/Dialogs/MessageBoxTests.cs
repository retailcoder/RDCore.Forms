using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using RDCore.Forms.Avalonia.Dialogs;
using UI = RDCore.Forms.Tests.Headless.Headless;
using RDCore.SDK.Runtime.Abstract.StdLib;
using Path = Avalonia.Controls.Shapes.Path;

namespace RDCore.Forms.Tests.Dialogs;

/// <summary>
/// The dialog of a <c>MsgBox</c> in an RDForms application: the buttons, the icon, the default button and the layout its style asks for, and how it closes.
/// </summary>
[TestClass]
public sealed class MessageBoxTests
{
    // The dialog shown on its own, what the test does to it, and the answer it closed with, or null when it is still open.
    private static (VBMsgBoxResult? Answer, bool Open) Answer(VBMsgBoxStyle style, Action<RDFormsMessageBox> act, string culture = "en-US")
        => UI.Run(() =>
        {
            var previous = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var dialog = new RDFormsMessageBox("Save the changes?", "Project1", style);
            CultureInfo.CurrentUICulture = previous;
            var shown = dialog.ShowAsync();
            Dispatcher.UIThread.RunJobs();
            act(dialog);
            Dispatcher.UIThread.RunJobs();
            var open = dialog.IsVisible;
            if (open)
                dialog.Choose(dialog.Choices[0]);

            Dispatcher.UIThread.RunJobs();
            return (open ? null : (VBMsgBoxResult?)UI.ResultOf(shown), open);
        });

    // A key that closes the dialog is released on no window.
    private static void Press(Window window, Key key, PhysicalKey physical, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, physical, null);
        if (window.IsVisible)
            window.KeyRelease(key, modifiers, physical, null);
    }

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    [TestMethod]
    [DataRow(VBMsgBoxStyle.VBOKOnly, new[] { "OK" }, "en-US")]
    [DataRow(VBMsgBoxStyle.VBOKCancel, new[] { "OK", "Cancel" }, "en-US")]
    [DataRow(VBMsgBoxStyle.VBAbortRetryIgnore, new[] { "_Abort", "_Retry", "_Ignore" }, "en-US")]
    [DataRow(VBMsgBoxStyle.VBYesNoCancel, new[] { "_Yes", "_No", "Cancel" }, "en-US")]
    [DataRow(VBMsgBoxStyle.VBYesNo, new[] { "_Yes", "_No" }, "en-US")]
    [DataRow(VBMsgBoxStyle.VBRetryCancel, new[] { "_Retry", "Cancel" }, "en-US")]
    [DataRow(VBMsgBoxStyle.VBYesNoCancel, new[] { "_Oui", "_Non", "Annuler" }, "fr-CA")]
    [DataRow(VBMsgBoxStyle.VBAbortRetryIgnore, new[] { "_Abandonner", "_Recommencer", "_Ignorer" }, "fr-CA")]
    public void TheButtons_AreTheStylesInItsOrder_WithTheCaptionsOfTheUsersLanguage(VBMsgBoxStyle style, string[] captions, string culture)
    {
        var shown = UI.Run(() =>
        {
            var previous = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var captions = new RDFormsMessageBox("Prompt", "Title", style).Buttons.Select(button => (string)button.Content!).ToArray();
            CultureInfo.CurrentUICulture = previous;
            return captions;
        });

        CollectionAssert.AreEqual(captions, shown);
    }

    [TestMethod]
    [DataRow(VBMsgBoxStyle.VBYesNoCancel, 0)]
    [DataRow(VBMsgBoxStyle.VBYesNoCancel | VBMsgBoxStyle.VBDefaultButton2, 1)]
    [DataRow(VBMsgBoxStyle.VBYesNoCancel | VBMsgBoxStyle.VBDefaultButton3, 2)]
    public void TheDefaultButton_HasTheFocus_AndAnswersEnter(VBMsgBoxStyle style, int index)
    {
        var (focused, isDefault, answer) = UI.Run(() =>
        {
            var dialog = new RDFormsMessageBox("Prompt", "Title", style);
            var shown = dialog.ShowAsync();
            Dispatcher.UIThread.RunJobs();
            var state = (dialog.Buttons[index].IsFocused, dialog.Buttons.Select(button => button.IsDefault).ToArray());
            Press(dialog, Key.Enter, PhysicalKey.Enter);
            Dispatcher.UIThread.RunJobs();
            return (state.IsFocused, state.Item2, shown.IsCompleted ? UI.ResultOf(shown) : (VBMsgBoxResult?)null);
        });

        Assert.IsTrue(focused);
        CollectionAssert.AreEqual(Enumerable.Range(0, isDefault.Length).Select(i => i == index).ToArray(), isDefault);
        Assert.AreEqual(VBMsgBoxButtons.ChoicesOf(style)[index], answer);
    }

    [TestMethod]
    public void AButton_AnswersItsChoice()
    {
        var (answer, open) = Answer(VBMsgBoxStyle.VBYesNoCancel, dialog => Click(dialog.Buttons[1]));

        Assert.IsFalse(open);
        Assert.AreEqual(VBMsgBoxResult.VBNo, answer);
    }

    [TestMethod]
    [DataRow(VBMsgBoxStyle.VBOKOnly, VBMsgBoxResult.VBOK)]
    [DataRow(VBMsgBoxStyle.VBOKCancel, VBMsgBoxResult.VBCancel)]
    [DataRow(VBMsgBoxStyle.VBYesNoCancel | VBMsgBoxStyle.VBDefaultButton1, VBMsgBoxResult.VBCancel)]
    public void EscAndTheCloseButton_AnswerCancel_OrOKWhenItIsTheOnlyButton(VBMsgBoxStyle style, VBMsgBoxResult expected)
    {
        Assert.AreEqual(expected, Answer(style, dialog => Press(dialog, Key.Escape, PhysicalKey.Escape)).Answer, "Esc");
        Assert.AreEqual(expected, Answer(style, dialog => dialog.Close()).Answer, "the close button");
    }

    [TestMethod]
    [DataRow(VBMsgBoxStyle.VBYesNo)]
    [DataRow(VBMsgBoxStyle.VBAbortRetryIgnore)]
    public void ADialogWithNoCancel_StaysOpen_OnEscAndOnItsCloseButton(VBMsgBoxStyle style)
    {
        Assert.IsTrue(Answer(style, dialog => Press(dialog, Key.Escape, PhysicalKey.Escape)).Open, "Esc");
        Assert.IsTrue(Answer(style, dialog => dialog.Close()).Open, "the close button");
    }

    [TestMethod]
    public void TheHelpButton_AsksForHelp_AndTheDialogStaysOpen()
    {
        var asked = 0;
        var (answer, open) = Answer(VBMsgBoxStyle.VBOKCancel | VBMsgBoxStyle.VBMsgBoxHelpButton, dialog =>
        {
            dialog.HelpRequested += (_, _) => asked++;
            Click(dialog.HelpButton!);
            Press(dialog, Key.F1, PhysicalKey.F1);
        });

        Assert.AreEqual(2, asked);
        Assert.IsTrue(open);
        Assert.IsNull(answer);
        Assert.IsNull(UI.Run(() => new RDFormsMessageBox("Prompt", "Title", VBMsgBoxStyle.VBOKCancel).HelpButton), "no Help button unless asked");
    }

    [TestMethod]
    [DataRow(VBMsgBoxStyle.VBCritical, typeof(Ellipse))]
    [DataRow(VBMsgBoxStyle.VBQuestion, typeof(Ellipse))]
    [DataRow(VBMsgBoxStyle.VBInformation, typeof(Ellipse))]
    [DataRow(VBMsgBoxStyle.VBExclamation, typeof(Path))]
    public void TheIcon_IsTheOneTheStyleAsksFor(VBMsgBoxStyle style, Type shape)
    {
        var (shapes, none) = UI.Run(() =>
        {
            var dialog = new RDFormsMessageBox("Prompt", "Title", style);
            var plain = new RDFormsMessageBox("Prompt", "Title", VBMsgBoxStyle.VBOKOnly);
            dialog.Show();
            plain.Show();
            var result = (dialog.GetVisualDescendants().OfType<Shape>().Select(s => s.GetType()).ToArray(),
                plain.GetVisualDescendants().OfType<Shape>().Count());
            dialog.Close();
            plain.Close();
            return result;
        });

        CollectionAssert.Contains(shapes, shape);
        Assert.AreEqual(0, none, "no icon unless asked");
    }

    [TestMethod]
    public void TheLayoutFlags_AlignTheMessageRight_AndMirrorTheDialog()
    {
        var (alignment, direction, plainAlignment, plainDirection) = UI.Run(() =>
        {
            var mirrored = new RDFormsMessageBox("Prompt", "Title", VBMsgBoxStyle.VBMsgBoxRight | VBMsgBoxStyle.VBMsgBoxRtlReading);
            var plain = new RDFormsMessageBox("Prompt", "Title", VBMsgBoxStyle.VBOKOnly);
            mirrored.Show();
            plain.Show();
            var result = (Message(mirrored).TextAlignment, mirrored.FlowDirection, Message(plain).TextAlignment, plain.FlowDirection);
            mirrored.Close();
            plain.Close();
            return result;
        });

        Assert.AreEqual(TextAlignment.Right, alignment);
        Assert.AreEqual(FlowDirection.RightToLeft, direction);
        Assert.AreEqual(TextAlignment.Left, plainAlignment);
        Assert.AreEqual(FlowDirection.LeftToRight, plainDirection);
    }

    [TestMethod]
    public void AMessageOfSeveralLines_BreaksAtEachOfItsSeparators()
        => Assert.AreEqual("one\ntwo\nthree\nfour", UI.Run(() =>
        {
            var dialog = new RDFormsMessageBox("one\rtwo\nthree\r\nfour", "Title", VBMsgBoxStyle.VBOKOnly);
            dialog.Show();
            var text = Message(dialog).Text;
            dialog.Close();
            return text;
        }));

    [TestMethod]
    public void CtrlC_CopiesTheDialog_AsWindowsCopiesAMessageBox()
    {
        var text = UI.Run(() =>
        {
            var previous = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var copied = new RDFormsMessageBox("Save the changes?", "Project1", VBMsgBoxStyle.VBYesNo | VBMsgBoxStyle.VBMsgBoxHelpButton).CopyText;
            CultureInfo.CurrentUICulture = previous;
            return copied;
        });

        const string rule = "---------------------------";
        Assert.AreEqual(string.Join(Environment.NewLine, rule, "Project1", rule, "Save the changes?", rule, "Yes   No   Help   ", rule, string.Empty), text);
    }

    [TestMethod]
    public void ADialogShownOverAWindow_IsOwnedByIt_AndOutOfTheTaskbar()
    {
        var (owned, inTaskbar, answer) = UI.Run(() =>
        {
            var owner = new Window();
            owner.Show();
            var dialog = new RDFormsMessageBox("Prompt", "Title", VBMsgBoxStyle.VBOKCancel);
            var shown = dialog.ShowAsync(owner);
            Dispatcher.UIThread.RunJobs();
            var state = (ReferenceEquals(dialog.Owner, owner), dialog.ShowInTaskbar);
            Click(dialog.Buttons[0]);
            Dispatcher.UIThread.RunJobs();
            owner.Close();
            return (state.Item1, state.ShowInTaskbar, shown.IsCompleted ? UI.ResultOf(shown) : (VBMsgBoxResult?)null);
        });

        Assert.IsTrue(owned);
        Assert.IsFalse(inTaskbar);
        Assert.AreEqual(VBMsgBoxResult.VBOK, answer);
    }

    private static TextBlock Message(Window dialog)
        => dialog.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text is { Length: > 1 } && block.FindAncestorOfType<Button>() is null);
}

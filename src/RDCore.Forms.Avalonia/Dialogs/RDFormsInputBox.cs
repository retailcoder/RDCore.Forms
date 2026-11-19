using System.Resources;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Dialogs;

/// <summary>
/// The dialog of an <c>InputBox</c> (<strong>MS-VBAL §6.1.2.8.1.12</strong>) in an RDForms application, laid out as Windows lays out VBA's: the message
/// at the top left, the <em>OK</em> and <em>Cancel</em> buttons stacked on its right, and the text box across the bottom.
/// </summary>
/// <remarks>
/// <kbd>Enter</kbd> and <em>OK</em> answer what is in the text box; <kbd>Esc</kbd>, <em>Cancel</em> and the close button cancel it. The dialog has a
/// <em>Help</em> button below <em>Cancel</em> when the program names a help topic, and <kbd>F1</kbd> asks for it as the button does.
/// </remarks>
public sealed class RDFormsInputBox : Window
{
    private static readonly ResourceManager Texts = new("RDCore.Forms.Avalonia.Dialogs.DialogTexts", typeof(RDFormsInputBox).Assembly);

    private const double PromptWidth = 270;
    private const double TextBoxHeight = 24;

    private readonly int? xPos;
    private readonly int? yPos;
    private readonly TextBlock message;
    private bool answered;

    /// <summary>
    /// A dialog that asks for a line of text.
    /// </summary>
    /// <param name="prompt">The message. Its lines are separated by a carriage return, a line feed, or both.</param>
    /// <param name="title">The title of the dialog: the project's name when the program gives none, which is the caller's to give.</param>
    /// <param name="defaultText">The text the text box starts with, selected.</param>
    /// <param name="xPos">Where the dialog's left edge is, in twips from the left of the screen; <see langword="null"/> to centre it across.</param>
    /// <param name="yPos">
    /// Where the dialog's top edge is, in twips from the top of the screen; <see langword="null"/> to put it a third of the way down.
    /// </param>
    /// <param name="hasHelp">Whether the program names a help topic, which a <em>Help</em> button and <kbd>F1</kbd> ask for.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="prompt"/>, <paramref name="title"/> or <paramref name="defaultText"/> is <see langword="null"/>.
    /// </exception>
    public RDFormsInputBox(string prompt, string title, string defaultText = "", int? xPos = null, int? yPos = null, bool hasHelp = false)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(defaultText);

        Prompt = prompt;
        Title = title;
        this.xPos = xPos;
        this.yPos = yPos;

        CanResize = false;
        CanMinimize = false;
        CanMaximize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
        RequestedThemeVariant = ThemeVariant.Light;

        message = new TextBlock { Text = DialogText.Lines(prompt), TextWrapping = TextWrapping.Wrap, Width = PromptWidth };
        OkButton = RDFormsMessageBox.ButtonOf(Texts.GetString("OK")!, isDefault: true, Confirm);
        CancelButton = RDFormsMessageBox.ButtonOf(Texts.GetString("Cancel")!, isDefault: false, Cancel);
        var buttons = new StackPanel { Spacing = DialogText.ButtonSpacing, Margin = new Thickness(DialogText.Margin / 2, 0, 0, 0) };
        buttons.Children.Add(OkButton);
        buttons.Children.Add(CancelButton);
        if (hasHelp)
        {
            HelpButton = RDFormsMessageBox.ButtonOf(Texts.GetString("Help")!, isDefault: false, RaiseHelp);
            buttons.Children.Add(HelpButton);
        }

        Input = new TextBox { Text = defaultText, MinHeight = TextBoxHeight, Margin = new Thickness(0, DialogText.Margin / 2, 0, 0) };
        var top = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Right);
        top.Children.Add(buttons);
        top.Children.Add(message);
        var layout = new DockPanel { Margin = new Thickness(DialogText.Margin / 2) };
        DockPanel.SetDock(Input, Dock.Bottom);
        layout.Children.Add(Input);
        layout.Children.Add(top);
        Content = layout;

        Paint();
        Opened += (_, _) =>
        {
            Place();
            Input.Focus();
            Input.SelectAll();
        };
    }

    /// <summary>
    /// The message.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// The text box the answer is typed in.
    /// </summary>
    public TextBox Input { get; }

    /// <summary>
    /// The <em>OK</em> button, which answers <kbd>Enter</kbd>.
    /// </summary>
    public Button OkButton { get; }

    /// <summary>
    /// The <em>Cancel</em> button.
    /// </summary>
    public Button CancelButton { get; }

    /// <summary>
    /// The <em>Help</em> button, or <see langword="null"/> when the program names no help topic.
    /// </summary>
    public Button? HelpButton { get; }

    /// <summary>
    /// What was in the text box when the dialog was confirmed, a zero-length string when nothing was; <see langword="null"/> while it is open, and once it
    /// is cancelled.
    /// </summary>
    public string? Result { get; private set; }

    /// <summary>
    /// Raised when the <em>Help</em> button is clicked, or <kbd>F1</kbd> is pressed in a dialog that has one: the program's help topic is the caller's to
    /// open. The dialog stays open.
    /// </summary>
    public event EventHandler? HelpRequested;

    /// <summary>
    /// Whether the dialog keeps the system's theme, as a form does with <c>App.KeepSystemTheme</c>: its colours are then the dark palette under a dark theme.
    /// It is drawn in the light palette otherwise, as Windows draws it.
    /// </summary>
    public bool KeepSystemTheme
    {
        get => RequestedThemeVariant != ThemeVariant.Light;
        set
        {
            if (value)
                ClearValue(RequestedThemeVariantProperty);
            else
                RequestedThemeVariant = ThemeVariant.Light;
        }
    }

    /// <summary>
    /// Shows the dialog, and waits for it to be confirmed or cancelled.
    /// </summary>
    /// <param name="owner">The window the dialog is shown over, which it disables until it is closed; <see langword="null"/> to show it on its own.</param>
    /// <returns>What was typed, a zero-length string when nothing was, or <see langword="null"/> when the dialog was cancelled.</returns>
    public async Task<string?> ShowAsync(Window? owner = null)
    {
        if (owner is null)
        {
            var closed = new TaskCompletionSource();
            Closed += (_, _) => closed.TrySetResult();
            Show();
            await closed.Task.ConfigureAwait(true);
        }
        else
        {
            ShowInTaskbar = false;
            await ShowDialog(owner).ConfigureAwait(true);
        }

        return Result;
    }

    /// <summary>
    /// Closes the dialog with what is in its text box, as <em>OK</em> does.
    /// </summary>
    public void Confirm()
    {
        answered = true;
        Result = Input.Text ?? string.Empty;
        Close();
    }

    /// <summary>
    /// Closes the dialog with no answer, as <em>Cancel</em> does.
    /// </summary>
    public void Cancel()
    {
        answered = true;
        Result = null;
        Close();
    }

    /// <summary>
    /// Where a dialog of a size is placed on a screen, as <c>InputBox</c> places it: its left edge <paramref name="xPos"/> twips from the screen's left, or
    /// centred across it; its top edge <paramref name="yPos"/> twips from the screen's top, or a third of the way down what the screen leaves below it.
    /// </summary>
    /// <param name="xPos">The left edge, in twips from the left of the screen; <see langword="null"/> to centre the dialog across.</param>
    /// <param name="yPos">The top edge, in twips from the top of the screen; <see langword="null"/> to put the dialog a third of the way down.</param>
    /// <param name="screen">The screen's bounds, in its pixels.</param>
    /// <param name="scaling">How many of the screen's pixels a device-independent pixel is.</param>
    /// <param name="dialog">The dialog's size, in the screen's pixels.</param>
    /// <returns>The position of the dialog's top-left corner, in the screen's pixels.</returns>
    public static PixelPoint PositionOf(int? xPos, int? yPos, PixelRect screen, double scaling, PixelSize dialog)
    {
        var x = xPos is { } left ? screen.X + (int)Math.Round(left / Units.TwipsPerDip * scaling) : screen.X + (screen.Width - dialog.Width) / 2;
        var y = yPos is { } top ? screen.Y + (int)Math.Round(top / Units.TwipsPerDip * scaling) : screen.Y + (screen.Height - dialog.Height) / 3;
        return new PixelPoint(x, y);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Cancel();
            e.Handled = true;
        }
        else if (e.Key == Key.F1 && HelpButton is not null)
        {
            RaiseHelp();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // The close button cancels the dialog.
        if (!answered)
        {
            answered = true;
            Result = null;
        }

        base.OnClosing(e);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualThemeVariantProperty)
            Paint();
    }

    private void RaiseHelp() => HelpRequested?.Invoke(this, EventArgs.Empty);

    // On the screen the dialog opens on; a platform with no screen, the headless one, leaves it where it is.
    private void Place()
    {
        if ((Screens.ScreenFromWindow(this) ?? Screens.Primary) is not { } screen)
            return;

        var scaling = screen.Scaling;
        var size = new PixelSize((int)Math.Ceiling(Bounds.Width * scaling), (int)Math.Ceiling(Bounds.Height * scaling));
        Position = PositionOf(xPos, yPos, screen.Bounds, scaling, size);
    }

    private void Paint()
    {
        if (message is null)
            return;

        Background = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.ButtonFace), ActualThemeVariant);
        message.Foreground = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.ButtonText), ActualThemeVariant);
    }
}

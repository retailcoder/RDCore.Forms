using System.Resources;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;
using RDCore.SDK.Runtime.Abstract.StdLib;
using TextAlignment = Avalonia.Media.TextAlignment;

namespace RDCore.Forms.Avalonia.Dialogs;

/// <summary>
/// The dialog of a <c>MsgBox</c> (<strong>MS-VBAL §6.1.2.8.1.13</strong>) in an RDForms application, laid out as Windows lays out VBA's: the icon and
/// the message on the window's background, and the buttons on the right of a band of the button face below them.
/// </summary>
/// <remarks>
/// <para>
/// The style gives the buttons, in the order <see cref="VBMsgBoxButtons.ChoicesOf"/> gives them, the icon, the button that has the focus and answers
/// <kbd>Enter</kbd>, the <em>Help</em> button, the message's alignment and its reading order. <kbd>Esc</kbd> and the close button answer
/// <see cref="VBMsgBoxButtons.EscapeResultOf"/>, and do nothing on a dialog that has no such answer. <kbd>Ctrl</kbd>+<kbd>C</kbd> copies
/// <see cref="CopyText"/>.
/// </para>
/// <para>
/// The modality of the style is the caller's: <see cref="ShowAsync"/> shows the dialog over the window it is given, or on its own.
/// </para>
/// </remarks>
public sealed class RDFormsMessageBox : Window
{
    private static readonly ResourceManager Texts = new("RDCore.Forms.Avalonia.Dialogs.DialogTexts", typeof(RDFormsMessageBox).Assembly);

    private readonly VBMsgBoxResult? escapeResult;
    private readonly Border content;
    private readonly Border band;
    private readonly TextBlock message;

    /// <summary>
    /// A dialog of a message, with the buttons, the icon and the layout its style asks for.
    /// </summary>
    /// <param name="prompt">The message. Its lines are separated by a carriage return, a line feed, or both.</param>
    /// <param name="title">The title of the dialog: the project's name when the program gives none, which is the caller's to give.</param>
    /// <param name="style">The style the program asked for: the buttons, the icon, the default button and the layout, summed.</param>
    /// <exception cref="ArgumentNullException"><paramref name="prompt"/> or <paramref name="title"/> is <see langword="null"/>.</exception>
    public RDFormsMessageBox(string prompt, string title, VBMsgBoxStyle style)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(title);

        Prompt = prompt;
        Style = style;
        Title = title;
        Choices = VBMsgBoxButtons.ChoicesOf(style);
        escapeResult = VBMsgBoxButtons.EscapeResultOf(style);

        CanResize = false;
        CanMinimize = false;
        CanMaximize = false;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FlowDirection = VBMsgBoxButtons.IsRightToLeft(style) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        RequestedThemeVariant = ThemeVariant.Light;

        message = new TextBlock
        {
            Text = DialogText.Lines(prompt),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = DialogText.MaxMessageWidth,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = VBMsgBoxButtons.IsRightAligned(style) ? TextAlignment.Right : TextAlignment.Left,
        };

        var body = new DockPanel { Margin = new Thickness(DialogText.Margin) };
        if (DialogIcons.Of(VBMsgBoxButtons.IconOf(style)) is { } icon)
        {
            icon.Margin = new Thickness(0, 0, DialogText.Margin / 2, 0);
            icon.VerticalAlignment = VerticalAlignment.Top;
            DockPanel.SetDock(icon, Dock.Left);
            body.Children.Add(icon);
        }

        body.Children.Add(message);
        content = new Border { Child = body, MinWidth = DialogText.MinWidth };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = DialogText.ButtonSpacing,
        };
        var focused = VBMsgBoxButtons.DefaultChoiceOf(style);
        Buttons = [.. Choices.Select((choice, index) => ButtonOf(TextOf(choice), index == focused, () => Choose(choice)))];
        foreach (var button in Buttons)
            buttons.Children.Add(button);

        if (VBMsgBoxButtons.HasHelpButton(style))
        {
            HelpButton = ButtonOf(Texts.GetString("Help")!, isDefault: false, RaiseHelp);
            buttons.Children.Add(HelpButton);
        }

        band = new Border { Child = buttons, Padding = new Thickness(DialogText.Margin, DialogText.Margin / 2) };
        var layout = new DockPanel();
        DockPanel.SetDock(band, Dock.Bottom);
        layout.Children.Add(band);
        layout.Children.Add(content);
        Content = layout;

        Paint();
        Opened += (_, _) => Buttons[focused].Focus();
    }

    /// <summary>
    /// The message.
    /// </summary>
    public string Prompt { get; }

    /// <summary>
    /// The style the program asked for.
    /// </summary>
    public VBMsgBoxStyle Style { get; }

    /// <summary>
    /// The answers of the dialog's buttons, in the order they are shown; the <em>Help</em> button answers nothing, and is not one of them.
    /// </summary>
    public IReadOnlyList<VBMsgBoxResult> Choices { get; }

    /// <summary>
    /// The buttons that answer the dialog, one for each of <see cref="Choices"/>, in its order.
    /// </summary>
    public IReadOnlyList<Button> Buttons { get; }

    /// <summary>
    /// The <em>Help</em> button, or <see langword="null"/> when the style asks for none.
    /// </summary>
    public Button? HelpButton { get; }

    /// <summary>
    /// The button that closed the dialog, or <see langword="null"/> while it is open.
    /// </summary>
    public VBMsgBoxResult? Result { get; private set; }

    /// <summary>
    /// Raised when the <em>Help</em> button is clicked, or <kbd>F1</kbd> is pressed: the program's help topic is the caller's to open. The dialog stays open.
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
    /// The dialog as text, as Windows copies a message box with <kbd>Ctrl</kbd>+<kbd>C</kbd>: its title, its message and its buttons' captions, between
    /// dashed lines.
    /// </summary>
    public string CopyText => DialogText.CopyOf(Title ?? string.Empty, Prompt,
        [.. Buttons.Select(button => button.Content as string ?? string.Empty), .. HelpButton is { Content: string help } ? [help] : Array.Empty<string>()]);

    /// <summary>
    /// Shows the dialog, and waits for one of its buttons to close it.
    /// </summary>
    /// <param name="owner">The window the dialog is shown over, which it disables until it is closed; <see langword="null"/> to show it on its own.</param>
    /// <returns>The button that closed the dialog.</returns>
    public async Task<VBMsgBoxResult> ShowAsync(Window? owner = null)
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
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            await ShowDialog(owner).ConfigureAwait(true);
        }

        return Result ?? escapeResult ?? Choices[VBMsgBoxButtons.DefaultChoiceOf(Style)];
    }

    /// <summary>
    /// Closes the dialog with the answer of one of its buttons, as clicking it does.
    /// </summary>
    /// <param name="choice">The answer.</param>
    /// <exception cref="ArgumentException">The dialog has no button that answers <paramref name="choice"/>.</exception>
    public void Choose(VBMsgBoxResult choice)
    {
        if (!Choices.Contains(choice))
            throw new ArgumentException($"The dialog has no {choice} button.", nameof(choice));

        Result = choice;
        Close();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && escapeResult is { } answer)
        {
            Choose(answer);
            e.Handled = true;
        }
        else if (e.Key == Key.F1)
        {
            RaiseHelp();
            e.Handled = true;
        }
        else if (e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control) && Clipboard is { } clipboard)
        {
            _ = clipboard.SetTextAsync(CopyText);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc/>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // The close button answers what Esc does; a dialog that Esc does not close stays open until a button closes it.
        if (Result is null)
        {
            if (escapeResult is { } answer)
                Result = answer;
            else
                e.Cancel = true;
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

    // The message's area is the window's colour, and the buttons' band the button face's, in the dialog's palette.
    private void Paint()
    {
        if (content is null)
            return;

        content.Background = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.Window), ActualThemeVariant);
        band.Background = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.ButtonFace), ActualThemeVariant);
        message.Foreground = FormPalette.ToBrush(FormColor.FromSystem(SystemColor.WindowText), ActualThemeVariant);
    }

    internal static Button ButtonOf(string caption, bool isDefault, Action click)
    {
        var button = new Button
        {
            Content = caption,
            IsDefault = isDefault,
            MinWidth = DialogText.ButtonWidth,
            HorizontalContentAlignment = HorizontalAlignment.Center,
        };
        button.Click += (_, e) =>
        {
            click();
            e.Handled = true;
        };
        return button;
    }

    private static string TextOf(VBMsgBoxResult choice) => Texts.GetString(choice switch
    {
        VBMsgBoxResult.VBCancel => "Cancel",
        VBMsgBoxResult.VBAbort => "Abort",
        VBMsgBoxResult.VBRetry => "Retry",
        VBMsgBoxResult.VBIgnore => "Ignore",
        VBMsgBoxResult.VBYes => "Yes",
        VBMsgBoxResult.VBNo => "No",
        _ => "OK",
    })!;
}

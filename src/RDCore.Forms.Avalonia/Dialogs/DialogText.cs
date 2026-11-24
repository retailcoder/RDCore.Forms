using System.Text;

namespace RDCore.Forms.Avalonia.Dialogs;

// The measures of the dialogs, in device-independent pixels as Windows lays its message boxes out at 96 DPI, and the text they show and copy.
internal static class DialogText
{
    public const double Margin = 22;
    public const double MinWidth = 160;
    public const double MaxMessageWidth = 520;
    public const double ButtonWidth = 88;
    public const double ButtonSpacing = 8;

    // A prompt's lines are separated by Chr(13), Chr(10) or both (MS-VBAL §6.1.2.8.1.13): a text block breaks a line at each.
    public static string Lines(string prompt) => prompt.Replace("\r\n", "\n").Replace('\r', '\n');

    // A caption's access key is shown underlined, and copied without its marker.
    public static string WithoutAccessKey(string caption) => caption.Replace("_", string.Empty);

    // As Windows copies a message box: dashed lines around its title, its message and its buttons, each caption followed by three spaces.
    public static string CopyOf(string title, string prompt, IEnumerable<string> captions)
    {
        const string rule = "---------------------------";
        var text = new StringBuilder()
            .AppendLine(rule).AppendLine(title)
            .AppendLine(rule).AppendLine(Lines(prompt).Replace("\n", Environment.NewLine))
            .AppendLine(rule);
        foreach (var caption in captions)
            text.Append(WithoutAccessKey(caption)).Append("   ");

        return text.AppendLine().AppendLine(rule).ToString();
    }
}

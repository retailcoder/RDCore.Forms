using Avalonia.Media;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// The colours a form's <see cref="SystemColor"/> entries are drawn in.
/// </summary>
/// <remarks>
/// The palette is Windows 11's default, as <c>GetSysColor</c> reads it with the default theme, on every platform, so a form looks
/// the same wherever it runs.
/// </remarks>
public static class FormPalette
{
    // By SystemColor, as red, green, blue.
    private static readonly uint[] windows =
    [
        0xC8C8C8, // ScrollBar
        0x000000, // Desktop
        0x99B4D1, // ActiveCaption
        0xBFCDDB, // InactiveCaption
        0xF0F0F0, // Menu
        0xFFFFFF, // Window
        0x646464, // WindowFrame
        0x000000, // MenuText
        0x000000, // WindowText
        0x000000, // CaptionText
        0xB4B4B4, // ActiveBorder
        0xF4F7FC, // InactiveBorder
        0xABABAB, // AppWorkspace
        0x0078D7, // Highlight
        0xFFFFFF, // HighlightText
        0xF0F0F0, // ButtonFace
        0xA0A0A0, // ButtonShadow
        0x6D6D6D, // GrayText
        0x000000, // ButtonText
        0x000000, // InactiveCaptionText
        0xFFFFFF, // ButtonHighlight
        0x696969, // ThreeDDarkShadow
        0xE3E3E3, // ThreeDLight
        0x000000, // InfoText
        0xFFFFE1, // InfoBackground
    ];

    /// <summary>
    /// The colour a form colour is drawn in.
    /// </summary>
    /// <param name="color">The form colour, an RGB colour or a system colour.</param>
    /// <returns>The colour, opaque.</returns>
    public static Color ToColor(FormColor color)
    {
        if (color.SystemColor is { } system)
        {
            var rgb = windows[(int)system];
            return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
        return Color.FromRgb(color.Red, color.Green, color.Blue);
    }

    /// <summary>
    /// A brush that paints a form colour.
    /// </summary>
    /// <param name="color">The form colour.</param>
    /// <returns>A solid brush of <see cref="ToColor"/>'s colour.</returns>
    public static IBrush ToBrush(FormColor color) => new SolidColorBrush(ToColor(color));
}

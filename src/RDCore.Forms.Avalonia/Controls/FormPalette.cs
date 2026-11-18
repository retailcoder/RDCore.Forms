using Avalonia.Media;
using Avalonia.Styling;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// The colours a form's <see cref="SystemColor"/> entries are drawn in.
/// </summary>
/// <remarks>
/// The light palette is Windows 11's default, as <c>GetSysColor</c> reads it with the default theme, on every platform, so a form
/// looks the same wherever it runs. <c>GetSysColor</c> gives the same colours in Windows' dark mode, so the dark palette is WinUI 3's
/// dark theme instead: each system colour is the colour of the resource that plays its part there, drawn over the dark background
/// where the resource is translucent. The accent, <c>Highlight</c>, is the same in both.
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

    // By SystemColor, as red, green, blue: the WinUI 3 dark theme resource that plays the colour's part (microsoft-ui-xaml,
    // controls/dev/CommonStyles/Common_themeresources_any.xaml, its "Default" dictionary, at 8463f451), a translucent one drawn over
    // SolidBackgroundFillColorBase (#202020). The accent is the system's in that file, so Highlight keeps the light palette's.
    private static readonly uint[] windowsDark =
    [
        0x575757, // ScrollBar: ControlStrongFillColorDisabled, #3FFFFFFF
        0x0A0A0A, // Desktop: SolidBackgroundFillColorBaseAlt
        0x202020, // ActiveCaption: SolidBackgroundFillColorBase
        0x1C1C1C, // InactiveCaption: SolidBackgroundFillColorSecondary
        0x2D2D2D, // Menu: ControlFillColorDefault, #0FFFFFFF, as ButtonFace (the light palette's Menu is its ButtonFace)
        0x1F1F1F, // Window: ControlFillColorInputActive, #B31E1E1E
        0x9A9A9A, // WindowFrame: ControlStrongStrokeColorDefault, #8BFFFFFF
        0xFFFFFF, // MenuText: TextFillColorPrimary
        0xFFFFFF, // WindowText: TextFillColorPrimary
        0xFFFFFF, // CaptionText: TextFillColorPrimary
        0x424242, // ActiveBorder: SurfaceStrokeColorDefault, #66757575
        0x1C1C1C, // InactiveBorder: CardStrokeColorDefaultSolid
        0x333333, // AppWorkspace: SolidBackgroundFillColorQuinary
        0x0078D7, // Highlight: the accent, as in the light palette
        0xFFFFFF, // HighlightText: TextOnAccentFillColorSelectedText
        0x2D2D2D, // ButtonFace: ControlFillColorDefault, #0FFFFFFF
        0x1D1D1D, // ButtonShadow: CardStrokeColorDefault, #19000000
        0x969696, // GrayText: TextFillColorTertiary, #87FFFFFF (4.7:1 on ButtonFace, as the light palette's 4.5:1)
        0xFFFFFF, // ButtonText: TextFillColorPrimary
        0xCCCCCC, // InactiveCaptionText: TextFillColorSecondary, #C5FFFFFF
        0x353535, // ButtonHighlight: ControlStrokeColorSecondary, #18FFFFFF
        0x0A0A0A, // ThreeDDarkShadow: SolidBackgroundFillColorBaseAlt
        0x303030, // ThreeDLight: ControlStrokeColorDefault, #12FFFFFF
        0xFFFFFF, // InfoText: TextFillColorPrimary
        0x2C2C2C, // InfoBackground: SolidBackgroundFillColorQuarternary
    ];

    /// <summary>
    /// The colour a form colour is drawn in, with the light palette.
    /// </summary>
    /// <param name="color">The form colour, an RGB colour or a system colour.</param>
    /// <returns>The colour, opaque.</returns>
    public static Color ToColor(FormColor color) => ToColor(color, ThemeVariant.Light);

    /// <summary>
    /// The colour a form colour is drawn in under a theme variant.
    /// </summary>
    /// <param name="color">The form colour, an RGB colour or a system colour.</param>
    /// <param name="variant">
    /// The theme variant. <see cref="ThemeVariant.Dark"/>, or a variant that inherits it, draws a system colour in the dark palette, the
    /// colours of Windows' dark theme as WinUI 3 gives them; any other variant, or none, draws it in the light palette.
    /// </param>
    /// <returns>The colour, opaque. An RGB colour is the same under every variant.</returns>
    public static Color ToColor(FormColor color, ThemeVariant? variant)
    {
        if (color.SystemColor is { } system)
        {
            var rgb = (IsDark(variant) ? windowsDark : windows)[(int)system];
            return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
        return Color.FromRgb(color.Red, color.Green, color.Blue);
    }

    /// <summary>
    /// A brush that paints a form colour, with the light palette.
    /// </summary>
    /// <param name="color">The form colour.</param>
    /// <returns>A solid brush of <see cref="ToColor(FormColor)"/>'s colour.</returns>
    public static IBrush ToBrush(FormColor color) => new SolidColorBrush(ToColor(color));

    /// <summary>
    /// A brush that paints a form colour under a theme variant.
    /// </summary>
    /// <param name="color">The form colour.</param>
    /// <param name="variant">The theme variant, as <see cref="ToColor(FormColor, ThemeVariant?)"/> reads it.</param>
    /// <returns>A solid brush of <see cref="ToColor(FormColor, ThemeVariant?)"/>'s colour.</returns>
    public static IBrush ToBrush(FormColor color, ThemeVariant? variant) => new SolidColorBrush(ToColor(color, variant));

    private static bool IsDark(ThemeVariant? variant)
    {
        for (var step = variant; step is not null; step = step.InheritVariant)
        {
            if (step == ThemeVariant.Dark)
                return true;
        }
        return false;
    }
}

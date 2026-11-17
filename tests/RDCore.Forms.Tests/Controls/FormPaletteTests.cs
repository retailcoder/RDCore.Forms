using Avalonia.Media;
using Avalonia.Styling;
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Controls;

[TestClass]
public sealed class FormPaletteTests
{
    // Windows 11's default palette, as GetSysColor reads it, by SystemColor.
    private static readonly string[] light =
    [
        "#C8C8C8", "#000000", "#99B4D1", "#BFCDDB", "#F0F0F0", "#FFFFFF", "#646464", "#000000", "#000000", "#000000", "#B4B4B4", "#F4F7FC",
        "#ABABAB", "#0078D7", "#FFFFFF", "#F0F0F0", "#A0A0A0", "#6D6D6D", "#000000", "#000000", "#FFFFFF", "#696969", "#E3E3E3", "#000000",
        "#FFFFE1",
    ];

    // The WinUI 3 dark theme resource each system colour takes, as Common_themeresources_any.xaml's "Default" dictionary has it (#AARRGGBB
    // or #RRGGBB). Highlight is the accent, which the file takes from the system, so it keeps the light palette's.
    private static readonly (SystemColor Color, string Resource, string Value)[] dark =
    [
        (SystemColor.ScrollBar, "ControlStrongFillColorDisabled", "#3FFFFFFF"),
        (SystemColor.Desktop, "SolidBackgroundFillColorBaseAlt", "#0A0A0A"),
        (SystemColor.ActiveCaption, "SolidBackgroundFillColorBase", "#202020"),
        (SystemColor.InactiveCaption, "SolidBackgroundFillColorSecondary", "#1C1C1C"),
        (SystemColor.Menu, "ControlFillColorDefault", "#0FFFFFFF"),
        (SystemColor.Window, "ControlFillColorInputActive", "#B31E1E1E"),
        (SystemColor.WindowFrame, "ControlStrongStrokeColorDefault", "#8BFFFFFF"),
        (SystemColor.MenuText, "TextFillColorPrimary", "#FFFFFF"),
        (SystemColor.WindowText, "TextFillColorPrimary", "#FFFFFF"),
        (SystemColor.CaptionText, "TextFillColorPrimary", "#FFFFFF"),
        (SystemColor.ActiveBorder, "SurfaceStrokeColorDefault", "#66757575"),
        (SystemColor.InactiveBorder, "CardStrokeColorDefaultSolid", "#1C1C1C"),
        (SystemColor.AppWorkspace, "SolidBackgroundFillColorQuinary", "#333333"),
        (SystemColor.Highlight, "the light palette's accent", "#0078D7"),
        (SystemColor.HighlightText, "TextOnAccentFillColorSelectedText", "#FFFFFF"),
        (SystemColor.ButtonFace, "ControlFillColorDefault", "#0FFFFFFF"),
        (SystemColor.ButtonShadow, "CardStrokeColorDefault", "#19000000"),
        (SystemColor.GrayText, "TextFillColorTertiary", "#87FFFFFF"),
        (SystemColor.ButtonText, "TextFillColorPrimary", "#FFFFFF"),
        (SystemColor.InactiveCaptionText, "TextFillColorSecondary", "#C5FFFFFF"),
        (SystemColor.ButtonHighlight, "ControlStrokeColorSecondary", "#18FFFFFF"),
        (SystemColor.ThreeDDarkShadow, "SolidBackgroundFillColorBaseAlt", "#0A0A0A"),
        (SystemColor.ThreeDLight, "ControlStrokeColorDefault", "#12FFFFFF"),
        (SystemColor.InfoText, "TextFillColorPrimary", "#FFFFFF"),
        (SystemColor.InfoBackground, "SolidBackgroundFillColorQuarternary", "#2C2C2C"),
    ];

    // SolidBackgroundFillColorBase: what a translucent resource is drawn over.
    private static readonly Color darkBase = Color.Parse("#202020");

    [TestMethod]
    public void TheLightPalette_IsWindows11sDefault_AndTheOneArgumentOverloadReadsIt()
    {
        var systems = Enum.GetValues<SystemColor>();
        Assert.HasCount(light.Length, systems);
        foreach (var system in systems)
        {
            var colour = FormColor.FromSystem(system);
            Assert.AreEqual(Color.Parse(light[(int)system]), FormPalette.ToColor(colour), system.ToString());
            Assert.AreEqual(FormPalette.ToColor(colour), FormPalette.ToColor(colour, ThemeVariant.Light), system.ToString());
            Assert.AreEqual(FormPalette.ToColor(colour), FormPalette.ToColor(colour, null), system.ToString());
        }
    }

    [TestMethod]
    public void TheDarkPalette_IsEachSystemColoursWinUIResource_DrawnOverTheDarkBackground()
    {
        CollectionAssert.AreEqual(Enum.GetValues<SystemColor>(), dark.Select(row => row.Color).ToArray());
        foreach (var (system, resource, value) in dark)
        {
            var expected = Over(darkBase, Color.Parse(value));
            Assert.AreEqual(expected, FormPalette.ToColor(FormColor.FromSystem(system), ThemeVariant.Dark), $"{system} ({resource})");
        }
    }

    [TestMethod]
    public void AVariantThatInheritsDark_ReadsTheDarkPalette_AndAnRgbColour_IsTheSameUnderEveryVariant()
    {
        var dim = new ThemeVariant("Dim", ThemeVariant.Dark);
        var face = FormColor.FromSystem(SystemColor.ButtonFace);
        var rgb = FormColor.FromRgb(0x12, 0x34, 0xAB);

        Assert.AreEqual(FormPalette.ToColor(face, ThemeVariant.Dark), FormPalette.ToColor(face, dim));
        Assert.AreNotEqual(FormPalette.ToColor(face, ThemeVariant.Light), FormPalette.ToColor(face, ThemeVariant.Dark));
        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark, dim, ThemeVariant.Default })
            Assert.AreEqual(Color.FromRgb(0x12, 0x34, 0xAB), FormPalette.ToColor(rgb, variant), variant.ToString());
    }

    // A translucent colour drawn over an opaque one, each channel rounded to the nearest.
    private static Color Over(Color under, Color over)
    {
        byte Channel(byte u, byte o) => (byte)Math.Round(u + (o - u) * over.A / 255.0, MidpointRounding.AwayFromZero);
        return Color.FromRgb(Channel(under.R, over.R), Channel(under.G, over.G), Channel(under.B, over.B));
    }
}

// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model;

/// <summary>
/// A font as a form holds it: every field a designer file writes for one.
/// </summary>
/// <param name="Name">The font's family name, such as <c>MS Sans Serif</c>.</param>
/// <param name="Size">The font's size in points. It is fractional: VB writes sizes such as 8.25 and 9.6.</param>
/// <param name="Weight">The font's weight, from 0 to 1000: 400 is normal and 700 is bold.</param>
/// <param name="Italic">Whether the font is italic.</param>
/// <param name="Underline">Whether the font is underlined.</param>
/// <param name="Strikethrough">Whether the font is struck through.</param>
/// <param name="Charset">The font's character set, as Windows numbers them: 0 is ANSI, 2 is Symbol.</param>
public readonly record struct FormFont(
    string Name, double Size, int Weight = FormFont.NormalWeight, bool Italic = false, bool Underline = false,
    bool Strikethrough = false, int Charset = 0)
{
    /// <summary>
    /// The weight of a normal font.
    /// </summary>
    public const int NormalWeight = 400;

    /// <summary>
    /// The weight of a bold font.
    /// </summary>
    public const int BoldWeight = 700;

    /// <summary>
    /// The font a VB form and its controls have when the form's file declares none: MS Sans Serif at 8.25 points, as measured in VB6.
    /// </summary>
    public static FormFont Default { get; } = new("MS Sans Serif", 8.25);

    /// <summary>
    /// Whether the font is bold: its weight is <see cref="BoldWeight"/> or more.
    /// </summary>
    public bool Bold => Weight >= BoldWeight;

    /// <summary>
    /// The font at the size VB6 draws it at, which is the size VB6 reads back from a font it holds, as measured.
    /// </summary>
    /// <returns>The font, with the size it is drawn at as its <see cref="Size"/>.</returns>
    /// <remarks>
    /// A font is drawn at a whole number of pixels, 96 to an inch, so at a multiple of 0.75 points: the size asked, in pixels and
    /// rounded half up, and at least 2 pixels. MS Sans Serif, MS Serif, Courier, System and Small Fonts are bitmap fonts, each drawn at
    /// sizes of its own up to 160 pixels: MS Sans Serif asked for 8 points, about 10.7 pixels, is drawn at 11 pixels, 8.25 points. Past
    /// 160 pixels, and past 153 for Small Fonts, they are drawn at whole pixels too.
    /// </remarks>
    public FormFont AtDrawnSize() => this with { Size = DrawnPixels(Name, Size) * 0.75 };

    private static double DrawnPixels(string? name, double size)
    {
        var pixels = Math.Round(size * 4 / 3, MidpointRounding.AwayFromZero);
        if (name is not null && BitmapSizes.TryGetValue(name, out var sizes) && pixels <= sizes.Last)
        {
            var drawn = sizes.Runs[0].Drawn;
            foreach (var (from, at) in sizes.Runs)
            {
                if (from > pixels)
                    break;
                drawn = at;
            }

            return drawn;
        }

        return Math.Max(2, pixels);
    }

    // Each bitmap font's sizes in pixels as VB6 measures them, native and p-code agreeing: from each size asked in a run, up to the
    // next run's, the font is drawn at the run's size. The sizes do not grow with the size asked everywhere: GDI scales a size of
    // the font's file to draw another, and MS Sans Serif asked for 143 pixels is drawn at 150, and asked for 144 at 144.
    private static readonly Dictionary<string, (int Last, (int From, int Drawn)[] Runs)> BitmapSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MS Sans Serif"] = (160,
        [
            (1, 11), (13, 13), (16, 16), (18, 18), (20, 20), (22, 22), (23, 23), (24, 24), (26, 26), (30, 30), (32, 32), (36, 36),
            (39, 39), (40, 40), (44, 44), (46, 46), (48, 48), (52, 52), (54, 54), (55, 55), (60, 60), (64, 64), (65, 65), (69, 69),
            (72, 72), (78, 80), (88, 90), (92, 92), (96, 96), (100, 100), (108, 108), (111, 115), (120, 120), (127, 128), (138, 138),
            (143, 150), (144, 144), (146, 150), (157, 160),
        ]),
        ["MS Serif"] = (160,
        [
            (1, 8), (9, 9), (11, 11), (13, 13), (16, 16), (18, 18), (20, 20), (22, 22), (23, 23), (24, 24), (26, 26), (27, 27),
            (30, 30), (32, 32), (36, 36), (39, 39), (40, 40), (44, 44), (45, 45), (46, 46), (48, 48), (52, 52), (54, 54), (55, 55),
            (60, 60), (64, 64), (65, 65), (69, 69), (72, 72), (78, 80), (88, 90), (92, 92), (96, 96), (100, 100), (108, 108),
            (111, 115), (120, 120), (127, 128), (138, 138), (143, 150), (144, 144), (146, 150), (157, 160),
        ]),
        ["Courier"] = (160,
        [
            (1, 13), (16, 16), (20, 20), (25, 25), (26, 26), (32, 32), (38, 39), (40, 40), (47, 48), (50, 50), (52, 52), (58, 60),
            (64, 64), (65, 65), (72, 75), (80, 80), (91, 91), (94, 100), (96, 96), (97, 100), (112, 112), (117, 120), (119, 125),
            (120, 120), (122, 125), (140, 140), (146, 150), (160, 160),
        ]),
        ["System"] = (160, [(1, 13), (23, 26), (36, 39), (49, 52), (62, 65), (75, 78), (88, 91), (101, 104)]),
        ["Small Fonts"] = (153,
        [
            (1, 3), (4, 4), (5, 5), (7, 7), (8, 8), (9, 9), (10, 10), (11, 11), (14, 14), (16, 16), (18, 18), (20, 20), (21, 21),
            (22, 22), (24, 24), (27, 27), (28, 28), (30, 30), (32, 32), (33, 33), (36, 36), (40, 40), (44, 44), (45, 45), (50, 50),
            (55, 55), (64, 60), (66, 66), (70, 70), (76, 77), (86, 88),
        ]),
    };
}

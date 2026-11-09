// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model;

/// <summary>
/// The unit a form or a container measures its contents' positions in, as VB's <c>ScaleMode</c> numbers them.
/// </summary>
public enum ScaleMode
{
    /// <summary>
    /// A scale the program defines with <c>ScaleLeft</c>, <c>ScaleTop</c>, <c>ScaleWidth</c> and <c>ScaleHeight</c>.
    /// </summary>
    User = 0,
    /// <summary>
    /// Twips: 1440 to the inch.
    /// </summary>
    Twip = 1,
    /// <summary>
    /// Points: 72 to the inch.
    /// </summary>
    Point = 2,
    /// <summary>
    /// Pixels of the screen.
    /// </summary>
    Pixel = 3,
    /// <summary>
    /// Characters: 120 twips across and 240 down.
    /// </summary>
    Character = 4,
    /// <summary>
    /// Inches.
    /// </summary>
    Inch = 5,
    /// <summary>
    /// Millimetres.
    /// </summary>
    Millimeter = 6,
    /// <summary>
    /// Centimetres.
    /// </summary>
    Centimeter = 7,
}

/// <summary>
/// Conversions between a <see cref="ScaleMode"/>'s units and twips.
/// </summary>
public static class ScaleModeExtensions
{
    /// <summary>
    /// The twips in a pixel at 96 dots per inch, the value the model converts pixels with.
    /// </summary>
    public const double TwipsPerPixel = 15;

    /// <summary>
    /// How many twips one unit of <paramref name="mode"/> is, across and down.
    /// </summary>
    /// <param name="mode">The scale mode.</param>
    /// <returns>
    /// The twips per unit, across and down. A <see cref="ScaleMode.Character"/> unit is not square: a character cell is 120 twips
    /// across and 240 down. A pixel is <see cref="TwipsPerPixel"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="mode"/> is <see cref="ScaleMode.User"/>, which has no fixed unit (its unit is the container's size over its
    /// scale), or is not a scale mode.
    /// </exception>
    public static (double Across, double Down) TwipsPerUnit(this ScaleMode mode) => mode switch
    {
        ScaleMode.Twip => (1, 1),
        ScaleMode.Point => (20, 20),
        ScaleMode.Pixel => (TwipsPerPixel, TwipsPerPixel),
        ScaleMode.Character => (120, 240),
        ScaleMode.Inch => (1440, 1440),
        ScaleMode.Millimeter => (1440 / 25.4, 1440 / 25.4),
        ScaleMode.Centimeter => (1440 / 2.54, 1440 / 2.54),
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
    };
}

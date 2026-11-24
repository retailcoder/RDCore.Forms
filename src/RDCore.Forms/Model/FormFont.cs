// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using RDCore.SDK.Runtime.Stdole;

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
/// <param name="IsNamed">
/// Whether the font's name was ever set; True by default. A <c>stdole.StdFont</c> no program has named reads its default name, <c>MS Sans Serif</c>, and is
/// drawn in the scalable face Windows maps no name to, at that face's sizes (<see cref="StdFontObject.IsNamed"/>). A font a form's file
/// declares is named.
/// </param>
public readonly record struct FormFont(
    string Name, double Size, int Weight = FormFont.NormalWeight, bool Italic = false, bool Underline = false,
    bool Strikethrough = false, int Charset = 0, bool IsNamed = true)
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
    /// The size is the one <see cref="StdFontObject.DrawnSize"/> gives for the font's name, or for no name where the font is not
    /// <see cref="IsNamed"/>: a whole number of pixels, or one of a bitmap font's own sizes, so MS Sans Serif asked for 8 points is drawn
    /// at 8.25. A size of 0 or less is drawn as the smallest size, and one too large to draw as the largest.
    /// </remarks>
    public FormFont AtDrawnSize() => this with { Size = (double)StdFontObject.DrawnSize(IsNamed ? Name ?? "" : null, Drawable(Size)) };

    /// <summary>
    /// Makes the <c>stdole.StdFont</c> that holds this font, as a VB6 control gives one for its <c>Font</c>.
    /// </summary>
    /// <param name="font">The new font, holding each of this font's values; <see langword="null"/> where a value is refused.</param>
    /// <returns>
    /// <see cref="StdFontFailure.None"/> where every value is taken; otherwise the failure of the first value a <see cref="StdFontObject"/>
    /// refuses, in the order name, size, weight, character set: a name longer than 31 characters, a size of 0 or less or of 24576 points
    /// or more, a weight outside 0 to 1000, a character set outside 0 to 255.
    /// </returns>
    /// <remarks>
    /// The new font's weight is 400 or 700, as a <see cref="StdFontObject"/> stores one. A font that is not <see cref="IsNamed"/> is
    /// given no name, so the new font is not named either.
    /// </remarks>
    public StdFontFailure TryToStdFont(out StdFontObject? font)
    {
        font = null;
        var made = new StdFontObject();
        var failure = IsNamed ? made.TrySetName(Name ?? "") : StdFontFailure.None;
        if (failure == StdFontFailure.None)
            failure = Size > 0 ? made.TrySetSize(Drawable(Size)) : StdFontFailure.InvalidPropertyValue;
        if (failure == StdFontFailure.None)
            failure = made.TrySetWeight(ToShort(Weight));
        if (failure == StdFontFailure.None)
            failure = made.TrySetCharset(ToShort(Charset));
        if (failure != StdFontFailure.None)
            return failure;
        made.Italic = Italic;
        made.Underline = Underline;
        made.Strikethrough = Strikethrough;
        font = made;
        return StdFontFailure.None;
    }

    /// <summary>
    /// Makes the font a <c>stdole.StdFont</c> holds, as a VB6 control takes one for its <c>Font</c>.
    /// </summary>
    /// <param name="font">The <c>stdole.StdFont</c>.</param>
    /// <returns>The font, at the size the <see cref="StdFontObject"/> is drawn at, and named where it is.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="font"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The size is read as <see cref="StdFontObject.SizeAsDrawn"/>, so the <see cref="StdFontObject"/> keeps the size a later name snaps
    /// from: drawing a font changes none of its values, as in VB6.
    /// </remarks>
    public static FormFont FromStdFont(StdFontObject font)
    {
        ArgumentNullException.ThrowIfNull(font);
        return new FormFont(font.Name, (double)font.SizeAsDrawn, font.Weight, font.Italic, font.Underline, font.Strikethrough, font.Charset,
            font.IsNamed);
    }

    // A size as the SDK takes one: more than 0, and within what a decimal holds; past it, or not a number, the font is drawn at its
    // largest size anyway.
    private static decimal Drawable(double size) => size switch
    {
        <= 0 => 0.0001m,
        < 1e28 => (decimal)size,
        _ => 1e28m,
    };

    // A value outside what a short holds is outside the StdFont's range too, and kept outside it so that it is refused.
    private static short ToShort(int value) => (short)Math.Clamp(value, -1, short.MaxValue);
}

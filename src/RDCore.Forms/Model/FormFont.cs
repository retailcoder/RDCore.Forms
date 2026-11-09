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
}

// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Globalization;

namespace RDCore.Forms.Model;

/// <summary>
/// A colour as a form holds it, which is an <c>OLE_COLOR</c>: an RGB colour, or an entry of the system palette that the platform
/// resolves when the form is drawn.
/// </summary>
/// <remarks>
/// As a Long, an RGB colour is <c>&amp;H00BBGGRR</c>, and a system colour is <c>&amp;H800000nn</c>, where <c>nn</c> is its
/// <see cref="Model.SystemColor"/> index. The model accepts no other Long as a colour.
/// </remarks>
public readonly record struct FormColor
{
    private const uint SystemFlag = 0x8000_0000;

    private readonly uint ole;

    private FormColor(uint ole) => this.ole = ole;

    /// <summary>
    /// The RGB colour <paramref name="red"/>, <paramref name="green"/>, <paramref name="blue"/>.
    /// </summary>
    /// <param name="red">The red component.</param>
    /// <param name="green">The green component.</param>
    /// <param name="blue">The blue component.</param>
    /// <returns>The colour.</returns>
    public static FormColor FromRgb(byte red, byte green, byte blue) => new((uint)(blue << 16 | green << 8 | red));

    /// <summary>
    /// The system palette's entry <paramref name="color"/>.
    /// </summary>
    /// <param name="color">The entry.</param>
    /// <returns>The colour.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="color"/> is not an entry of the system palette.</exception>
    public static FormColor FromSystem(SystemColor color)
    {
        if (!Enum.IsDefined(color))
            throw new ArgumentOutOfRangeException(nameof(color), color, null);
        return new(SystemFlag | (uint)color);
    }

    /// <summary>
    /// The colour an <c>OLE_COLOR</c> Long is, when it is one.
    /// </summary>
    /// <param name="ole">The Long.</param>
    /// <param name="color">The colour, when <paramref name="ole"/> is one.</param>
    /// <returns>
    /// <see langword="true"/> for an RGB colour (<c>&amp;H00000000</c> to <c>&amp;H00FFFFFF</c>) or a system colour
    /// (<c>&amp;H80000000</c> to <c>&amp;H80000018</c>); <see langword="false"/> for any other Long.
    /// </returns>
    public static bool TryFromOle(int ole, out FormColor color)
    {
        var bits = unchecked((uint)ole);
        var valid = (bits >> 24) switch
        {
            0 => true,
            0x80 => (bits & 0x00FF_FF00) == 0 && Enum.IsDefined((SystemColor)(bits & 0xFF)),
            _ => false,
        };
        color = valid ? new FormColor(bits) : default;
        return valid;
    }

    /// <summary>
    /// The black RGB colour, <c>&amp;H00000000</c>.
    /// </summary>
    public static FormColor Black => default;

    /// <summary>
    /// Whether the colour is an entry of the system palette.
    /// </summary>
    public bool IsSystem => (ole & SystemFlag) != 0;

    /// <summary>
    /// The system palette's entry, or <see langword="null"/> for an RGB colour.
    /// </summary>
    public SystemColor? SystemColor => IsSystem ? (SystemColor)(ole & 0xFF) : null;

    /// <summary>
    /// The red component of an RGB colour; 0 for a system colour.
    /// </summary>
    public byte Red => IsSystem ? (byte)0 : (byte)(ole & 0xFF);

    /// <summary>
    /// The green component of an RGB colour; 0 for a system colour.
    /// </summary>
    public byte Green => IsSystem ? (byte)0 : (byte)(ole >> 8 & 0xFF);

    /// <summary>
    /// The blue component of an RGB colour; 0 for a system colour.
    /// </summary>
    public byte Blue => IsSystem ? (byte)0 : (byte)(ole >> 16 & 0xFF);

    /// <summary>
    /// The colour as an <c>OLE_COLOR</c> Long, the value a program reads: negative for a system colour.
    /// </summary>
    /// <returns>The Long.</returns>
    public int ToOle() => unchecked((int)ole);

    /// <summary>
    /// The colour as VB writes it in a designer file, such as <c>&amp;H8000000F&amp;</c>.
    /// </summary>
    /// <returns>The colour's hexadecimal literal.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"&H{ole:X8}&");
}

/// <summary>
/// The entries of the system palette a <see cref="FormColor"/> can name, with their indexes.
/// </summary>
public enum SystemColor
{
    /// <summary>
    /// The scroll bars' gray area.
    /// </summary>
    ScrollBar = 0,
    /// <summary>
    /// The desktop.
    /// </summary>
    Desktop = 1,
    /// <summary>
    /// The active window's title bar.
    /// </summary>
    ActiveCaption = 2,
    /// <summary>
    /// An inactive window's title bar.
    /// </summary>
    InactiveCaption = 3,
    /// <summary>
    /// A menu's background.
    /// </summary>
    Menu = 4,
    /// <summary>
    /// A window's background.
    /// </summary>
    Window = 5,
    /// <summary>
    /// A window's frame.
    /// </summary>
    WindowFrame = 6,
    /// <summary>
    /// A menu's text.
    /// </summary>
    MenuText = 7,
    /// <summary>
    /// A window's text.
    /// </summary>
    WindowText = 8,
    /// <summary>
    /// The text of a title bar, and of a size box and a scroll bar's arrow box.
    /// </summary>
    CaptionText = 9,
    /// <summary>
    /// The active window's border.
    /// </summary>
    ActiveBorder = 10,
    /// <summary>
    /// An inactive window's border.
    /// </summary>
    InactiveBorder = 11,
    /// <summary>
    /// The background of a multiple-document interface application.
    /// </summary>
    AppWorkspace = 12,
    /// <summary>
    /// The background of a selected item.
    /// </summary>
    Highlight = 13,
    /// <summary>
    /// The text of a selected item.
    /// </summary>
    HighlightText = 14,
    /// <summary>
    /// The face of a button and of other three-dimensional elements.
    /// </summary>
    ButtonFace = 15,
    /// <summary>
    /// The shadow edge of a button and of other three-dimensional elements.
    /// </summary>
    ButtonShadow = 16,
    /// <summary>
    /// Disabled text.
    /// </summary>
    GrayText = 17,
    /// <summary>
    /// A button's text.
    /// </summary>
    ButtonText = 18,
    /// <summary>
    /// The text of an inactive window's title bar.
    /// </summary>
    InactiveCaptionText = 19,
    /// <summary>
    /// The highlight edge of a button and of other three-dimensional elements.
    /// </summary>
    ButtonHighlight = 20,
    /// <summary>
    /// The darkest shadow of three-dimensional elements.
    /// </summary>
    ThreeDDarkShadow = 21,
    /// <summary>
    /// The second-lightest edge of three-dimensional elements; <see cref="ButtonHighlight"/> is the lightest.
    /// </summary>
    ThreeDLight = 22,
    /// <summary>
    /// A tooltip's text.
    /// </summary>
    InfoText = 23,
    /// <summary>
    /// A tooltip's background.
    /// </summary>
    InfoBackground = 24,
}

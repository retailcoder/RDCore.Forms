// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model.VB;

/// <summary>
/// Whether a VB control is drawn flat or with a three-dimensional edge: its <c>Appearance</c>.
/// </summary>
public enum Appearance
{
    /// <summary>
    /// Flat.
    /// </summary>
    Flat = 0,
    /// <summary>
    /// Three-dimensional, the default.
    /// </summary>
    ThreeD = 1,
}

/// <summary>
/// The border of a VB control: its <c>BorderStyle</c>. A Shape's and a Line's are <see cref="ShapeBorderStyle"/>.
/// </summary>
public enum BorderStyle
{
    /// <summary>
    /// No border.
    /// </summary>
    None = 0,
    /// <summary>
    /// A single line.
    /// </summary>
    FixedSingle = 1,
}

/// <summary>
/// Whether a VB Label's or Shape's background is drawn: its <c>BackStyle</c>.
/// </summary>
public enum BackStyle
{
    /// <summary>
    /// The background is not drawn: what is behind the control shows through.
    /// </summary>
    Transparent = 0,
    /// <summary>
    /// The background is drawn in the control's <c>BackColor</c>.
    /// </summary>
    Opaque = 1,
}

/// <summary>
/// The state of a VB CheckBox: its <c>Value</c>, an Integer to a program.
/// </summary>
public enum CheckBoxValue
{
    /// <summary>
    /// Not checked.
    /// </summary>
    Unchecked = 0,
    /// <summary>
    /// Checked.
    /// </summary>
    Checked = 1,
    /// <summary>
    /// Grayed: neither checked nor unchecked.
    /// </summary>
    Grayed = 2,
}

/// <summary>
/// How a VB control aligns its text: its <c>Alignment</c>.
/// </summary>
public enum TextAlignment
{
    /// <summary>
    /// Aligned to the left, the default.
    /// </summary>
    LeftJustify = 0,
    /// <summary>
    /// Aligned to the right.
    /// </summary>
    RightJustify = 1,
    /// <summary>
    /// Centred.
    /// </summary>
    Center = 2,
}

/// <summary>
/// Where a VB form opens on the screen: its <c>StartUpPosition</c>.
/// </summary>
/// <remarks>
/// A form whose file has no <c>StartUpPosition</c> is <see cref="Manual"/>, as measured in VB6. VB6's designer writes
/// <see cref="WindowsDefault"/> for a form it adds.
/// </remarks>
public enum StartUpPosition
{
    /// <summary>
    /// At the position the form's file gives it.
    /// </summary>
    Manual = 0,
    /// <summary>
    /// Centred on the form that owns it.
    /// </summary>
    CenterOwner = 1,
    /// <summary>
    /// Centred on the screen.
    /// </summary>
    CenterScreen = 2,
    /// <summary>
    /// Where the window manager places a new window.
    /// </summary>
    WindowsDefault = 3,
}

/// <summary>
/// Which edge of its form a VB PictureBox is docked to: its <c>Align</c>.
/// </summary>
public enum Align
{
    /// <summary>
    /// Not docked.
    /// </summary>
    None = 0,
    /// <summary>
    /// Docked to the top.
    /// </summary>
    Top = 1,
    /// <summary>
    /// Docked to the bottom.
    /// </summary>
    Bottom = 2,
    /// <summary>
    /// Docked to the left.
    /// </summary>
    Left = 3,
    /// <summary>
    /// Docked to the right.
    /// </summary>
    Right = 4,
}

/// <summary>
/// The figure a VB Shape draws: its <c>Shape</c>.
/// </summary>
public enum ShapeType
{
    /// <summary>
    /// A rectangle, the default.
    /// </summary>
    Rectangle = 0,
    /// <summary>
    /// A square.
    /// </summary>
    Square = 1,
    /// <summary>
    /// An oval.
    /// </summary>
    Oval = 2,
    /// <summary>
    /// A circle.
    /// </summary>
    Circle = 3,
    /// <summary>
    /// A rectangle with rounded corners.
    /// </summary>
    RoundedRectangle = 4,
    /// <summary>
    /// A square with rounded corners.
    /// </summary>
    RoundedSquare = 5,
}

/// <summary>
/// How a VB ComboBox is drawn and behaves: its <c>Style</c>, set at design time and read-only to a program.
/// </summary>
public enum ComboBoxStyle
{
    /// <summary>
    /// A text box with a list that drops down, the default.
    /// </summary>
    DropdownCombo = 0,
    /// <summary>
    /// A text box with a list always shown.
    /// </summary>
    SimpleCombo = 1,
    /// <summary>
    /// A list that drops down, with no text box: its text is the selected item.
    /// </summary>
    DropdownList = 2,
}

/// <summary>
/// Whether a VB ListBox selects more than one item, and how: its <c>MultiSelect</c>, set at design time and read-only to a program.
/// </summary>
public enum ListBoxMultiSelect
{
    /// <summary>
    /// One item at a time, the default.
    /// </summary>
    None = 0,
    /// <summary>
    /// A click or the space bar selects or deselects an item.
    /// </summary>
    Simple = 1,
    /// <summary>
    /// Shift extends the selection, and Ctrl adds an item to it.
    /// </summary>
    Extended = 2,
}

/// <summary>
/// How a VB Shape, form or PictureBox fills a figure: its <c>FillStyle</c>.
/// </summary>
public enum FillStyle
{
    /// <summary>
    /// Filled with the fill colour.
    /// </summary>
    Solid = 0,
    /// <summary>
    /// Not filled, the default.
    /// </summary>
    Transparent = 1,
    /// <summary>
    /// Horizontal lines.
    /// </summary>
    HorizontalLine = 2,
    /// <summary>
    /// Vertical lines.
    /// </summary>
    VerticalLine = 3,
    /// <summary>
    /// Lines rising to the right.
    /// </summary>
    UpwardDiagonal = 4,
    /// <summary>
    /// Lines falling to the right.
    /// </summary>
    DownwardDiagonal = 5,
    /// <summary>
    /// Horizontal and vertical lines.
    /// </summary>
    Cross = 6,
    /// <summary>
    /// Lines rising and falling to the right.
    /// </summary>
    DiagonalCross = 7,
}

/// <summary>
/// How a VB Shape's border or a Line is drawn: its <c>BorderStyle</c>.
/// </summary>
public enum ShapeBorderStyle
{
    /// <summary>
    /// Not drawn.
    /// </summary>
    Transparent = 0,
    /// <summary>
    /// A solid line, the default.
    /// </summary>
    Solid = 1,
    /// <summary>
    /// Dashes.
    /// </summary>
    Dash = 2,
    /// <summary>
    /// Dots.
    /// </summary>
    Dot = 3,
    /// <summary>
    /// Dashes and dots.
    /// </summary>
    DashDot = 4,
    /// <summary>
    /// A dash and two dots.
    /// </summary>
    DashDotDot = 5,
    /// <summary>
    /// A solid line drawn inside the figure's bounds.
    /// </summary>
    InsideSolid = 6,
}

/// <summary>
/// The pointer shown over a VB form or control: its <c>MousePointer</c>.
/// </summary>
public enum MousePointer
{
    /// <summary>
    /// The pointer the object's class shows, the default.
    /// </summary>
    Default = 0,
    /// <summary>
    /// An arrow.
    /// </summary>
    Arrow = 1,
    /// <summary>
    /// A cross hair.
    /// </summary>
    Cross = 2,
    /// <summary>
    /// A text cursor.
    /// </summary>
    IBeam = 3,
    /// <summary>
    /// A small square in a square.
    /// </summary>
    Icon = 4,
    /// <summary>
    /// Arrows pointing in four directions.
    /// </summary>
    Size = 5,
    /// <summary>
    /// A double arrow pointing north-east and south-west.
    /// </summary>
    SizeNESW = 6,
    /// <summary>
    /// A double arrow pointing north and south.
    /// </summary>
    SizeNS = 7,
    /// <summary>
    /// A double arrow pointing north-west and south-east.
    /// </summary>
    SizeNWSE = 8,
    /// <summary>
    /// A double arrow pointing west and east.
    /// </summary>
    SizeWE = 9,
    /// <summary>
    /// An arrow pointing up.
    /// </summary>
    UpArrow = 10,
    /// <summary>
    /// An hourglass: the program is busy.
    /// </summary>
    Hourglass = 11,
    /// <summary>
    /// A circle with a line through it: no drop here.
    /// </summary>
    NoDrop = 12,
    /// <summary>
    /// An arrow and an hourglass: busy in the background.
    /// </summary>
    ArrowHourglass = 13,
    /// <summary>
    /// An arrow and a question mark.
    /// </summary>
    ArrowQuestion = 14,
    /// <summary>
    /// Arrows pointing in four directions, for moving.
    /// </summary>
    SizeAll = 15,
    /// <summary>
    /// The object's <c>MouseIcon</c>.
    /// </summary>
    Custom = 99,
}

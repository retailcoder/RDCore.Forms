# Drawing

A form and a PictureBox each have a drawing surface, a [`RDFormsDrawingLayer`](../api/RDCore.Forms.Avalonia.Controls.RDFormsDrawingLayer.yml): the destination of VB's drawing statements, `Line`, `Circle`, `PSet` and `Scale`, and of the `Cls`, `Point`, `Print`, `TextWidth`, `TextHeight` and `PaintPicture` methods. A program reaches it through the [`IDrawingSurface`](../api/RDCore.Forms.Drawing.IDrawingSurface.yml) interface, which [`DrawingSurfaces.Of`](../api/RDCore.Forms.Drawing.DrawingSurfaces.yml) gives for a shown form's root or PictureBox. The interface is part of `RDCore.Forms` and has no Avalonia types.

```csharp
using RDCore.Forms.Drawing;

// Line (10, 10)-Step(100, 50), , B
var surface = DrawingSurfaces.Of(form.Root)!;
surface.Line(new GraphicsPoint(10, 10), new GraphicsPoint(100, 50, IsRelative: true), box: LineBox.Box);
```

## Where it draws

- The surface covers the component's inside: a form's client area, and a PictureBox's inside its border.
- What it draws is under every control, Labels, Images, Shapes and Lines included. On a form and a PictureBox it is over the `Picture`.
- Coordinates are in the component's scale: its `ScaleMode`, from its `ScaleLeft` and `ScaleTop`. A point is drawn on the pixel nearest its twips, rounded half to even. [`DrawingScale`](../api/RDCore.Forms.Drawing.DrawingScale.yml) converts between a scale and twips.

## The scale

In a form's window, the form's scale and each PictureBox's follow VB6's rules ([`ScaleRules`](../api/RDCore.Forms.Drawing.ScaleRules.yml)). A designer file's values are read as it holds them.

|Change|Effect|
|---|---|
|`ScaleLeft`, `ScaleTop`, `ScaleWidth` or `ScaleHeight` set|`ScaleMode` becomes 0, User, even when the value is the one it has. The other three keep theirs.|
|`ScaleMode` set to a unit, 1 to 7|`ScaleLeft` and `ScaleTop` become 0, and `ScaleWidth` and `ScaleHeight` the inside in that unit.|
|`ScaleMode` set to 0|The four keep their values.|
|The size changes|In a unit, `ScaleWidth` and `ScaleHeight` are the inside. In a user scale the unit keeps its size: `ScaleWidth` and `ScaleHeight` change with the inside, and `ScaleLeft` and `ScaleTop` stay.|

- A `ScaleWidth` or `ScaleHeight` of 0 is error 380, and a negative one runs the scale right to left or bottom to top.
- A `ScaleMode` outside 0 to 7 is error 380, and the scale is unchanged. VB6 then reads the refused `ScaleMode` back; RDForms keeps the one it had.
- A change of `BorderStyle` or `Appearance` changes the inside as a change of size does (RDForms' own, not measured with VB6).
- A Line the form or the PictureBox holds stays where it is when a set changes the scale: its `X1`, `Y1`, `X2` and `Y2` are given again in the new scale. 200, 233.3333 in a PictureBox whose `ScaleMode` is pixels read 3000, 3500 once it is twips. A change of size leaves them as they are. A Line in a Frame is in the Frame's twips, which no scale changes (RDForms' own: a Frame has no scale).

## Statements and methods

|Member|Draws|`CurrentX`, `CurrentY` after|
|---|---|---|
|`Line`|A line from the first point, or from the current point, to the second. `B` draws a box, filled as `FillStyle` says; `BF` fills it with the line's colour. A box covers both corners.|The second point.|
|`Circle`|A circle, an ellipse (with an aspect), an arc (with a start and an end, in radians), or a sector. A negative start or end also draws a radius to that end, and with both negative the sector is closed. The radius is in horizontal units. An angle outside -2π to 2π is error 5.|The centre.|
|`PSet`|A point; a dot `DrawWidth` wide where that is more than 1.|The point.|
|`Scale`|Nothing. With two points it sets a user scale whose corners they are; with none it sets the scale back to twips.|The same point, in the new scale.|
|`Cls`|Clears the drawing, and keeps the `Picture`.|0, 0.|
|`Point`|Nothing. It returns the colour at a point: what is drawn there, or else the `Picture`'s pixel, or else the `BackColor`. It returns -1 outside the surface.|Unchanged.|

A point written with `Step` is measured from the current point, and a `Line`'s second point from its first. A colour left out is the component's `ForeColor`.

The current point is kept in whole twips from the inside's top left corner. A `CurrentX` or `CurrentY` set is rounded to the nearest twip, and a change of scale, by `Scale` or by a `ScaleMode`, `ScaleLeft` or `ScaleWidth` set, keeps the point where it is.

## Print, TextWidth and TextHeight

`Print` draws its output list in the component's `Font` and `ForeColor`, each line's top left corner on the pixel nearest the current point. [`IDrawingSurface.Print`](../api/RDCore.Forms.Drawing.IDrawingSurface.yml) takes the list as [`PrintItem`](../api/RDCore.Forms.Drawing.PrintItem.yml)s: texts, `Spc(n)` and `Tab(n)`, a comma being a `Tab` with no column.

A column is the font's average character width, and a print zone is 14 columns. Columns and zones count from the scale's 0, and each position is moved to the pixel nearest it.

|Item|`CurrentX`, `CurrentY` after|
|---|---|
|A text|`CurrentX` at its end, `CurrentY` on the pixel it is drawn on. A carriage return, a line feed, or both together start a new line within it.|
|`Spc(n)`|n columns on, to the column boundary at or after that.|
|`Tab(n)`|The start of column n, counted from 1. Where the current point is past it, the start of column n on the next line.|
|`Tab`, or a comma|The first zone boundary past the current point.|
|The end of a list with no `;` or `,` at its end|The scale's 0 across, a line down.|

`TextWidth` is the width of a text's longest line, and `TextHeight` its lines times the font's line height; an empty text is one line high and 0 wide. Both are in the component's units.

Where `FontTransparent` is False, each line's cell, its width and a line high, is filled with the `BackColor` before the text is drawn. `DrawMode` does not apply to text, and text is drawn with no smoothing.

MS Sans Serif's column and line height are VB6's at each size it is drawn at, up to 160 pixels: [Fonts](fonts.md) lists them.

Another font's column and line height are RDForms' own: the column is the OS/2 average width of the font file that draws the text, at its size, and the line is its ascent and descent together. A text's width is always the width of the font that draws it, so MS Sans Serif's text, drawn in a libre sans serif, has that font's widths, not VB6's.

## Erasing

|Set|The drawing|`CurrentX`, `CurrentY`|
|---|---|---|
|`Picture`, to a picture or to none|Erased; the new picture shows.|Kept.|
|`BackColor` on a PictureBox|Erased, even where the colour is the same.|Kept.|
|`BackColor` on a form|Erased where the colour changes, kept where it is the same.|Kept.|

## Pens and fills

- Lines are drawn as a Line control draws them: `DrawWidth` pixels wide, dashed as `DrawStyle` says where they are one pixel wide, and solid where they are wider. Transparent draws no line.
- A box's or a circle's fill is its `FillColor` in its `FillStyle`: solid, or lines eight pixels apart: horizontal ones on rows 3, 11, 19… and vertical ones on columns 4, 12, 20… from the surface's corner, wherever the figure is, as VB6 draws them. An arc is not filled; a closed sector is. The fill stops where the edge starts.
- There is no smoothing.

## DrawMode

`DrawMode` combines each pixel a line, a point or a fill covers with the pixel under it: what is drawn there, or else the `Picture`'s pixel, or else the `BackColor`. Each mode is the Windows raster operation of the same number, applied bit by bit to red, green and blue. P is the pen's colour (or the `FillColor`, for a fill) and D the colour under it.

|`DrawMode`|Name|Result|
|---|---|---|
|1|Blackness|Black|
|2|Not Merge Pen|Not (D Or P)|
|3|Mask Not Pen|D And Not P|
|4|Not Copy Pen|Not P|
|5|Mask Pen Not|P And Not D|
|6|Invert|Not D|
|7|Xor Pen|D Xor P|
|8|Not Mask Pen|Not (D And P)|
|9|Mask Pen|D And P|
|10|Not Xor Pen|Not (D Xor P)|
|11|Nop|D: nothing is drawn.|
|12|Merge Not Pen|D Or Not P|
|13|Copy Pen, the default|P|
|14|Merge Pen Not|P Or Not D|
|15|Merge Pen|D Or P|
|16|Whiteness|White|

Xor Pen drawn twice gives back what was there. Another value is error 380, and the `DrawMode` is kept.

## PaintPicture

`PaintPicture picture, x1, y1, width1, height1, x2, y2, width2, height2, opcode` draws a picture, or a rectangle of it, stretched to a rectangle of the surface ([`IDrawingSurface.PaintPicture`](../api/RDCore.Forms.Drawing.IDrawingSurface.yml)). It follows VB6's rules, as measured.

- Every position and size, the source's included, is in the component's scale, and is placed on the pixel nearest it. The picture's own size is its size at 96 pixels an inch.
- `x2` and `y2` default to 0, `width2` and `height2` to the rest of the picture from them, and `width1` and `height1` to the source's size.
- A negative `width1` or `height1` mirrors the picture, and so does a negative `width2` or `height2`; both negative draw it the right way round.
- A destination pixel takes the source pixel under its centre. A part of the source outside the picture draws nothing, and the destination is cut in proportion. A transparent part of the picture shows what is under it.
- `opcode` (`vbSrcCopy` by default) is the GDI ternary raster operation of its third byte, over the pattern, the source and what is there, on each of red, green and blue. The pattern is the component's fill: its `FillColor` where `FillStyle` is Solid, and its `FillColor` lines over its `BackColor` where it is hatched, in the phase of a box's fill. Where `FillStyle` is Transparent, an opcode that reads the pattern draws nothing.
- `DrawMode` plays no part. `CurrentX`, `CurrentY` and the `Picture` are kept, and `Cls` erases what `PaintPicture` drew.

|Case|Error|
|---|---|
|An empty picture, or one that is not a picture|481|
|A position or a size too large for a pixel|6|
|A width or a height under half a pixel|5|
|A source wholly left of or above the picture, or starting at its right or bottom edge|5|
|A source starting past that edge|None: nothing is drawn.|

> [!NOTE]
> **Not implemented.** A mirrored source that reaches past the picture's edge is drawn one or two pixels off VB6's: VB6 cuts the destination on another side than the one the source is cut on.

## AutoRedraw and Paint

The surface keeps what it draws whatever the component's `AutoRedraw`, and keeps it as the component shrinks and grows again.

|`AutoRedraw`|Paint|
|---|---|
|False, the default|Raised once the surface is shown, and each time the component's inside grows. One is raised for the changes made before it comes, so a window grown across and down at once raises one.|
|True|Not raised.|

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

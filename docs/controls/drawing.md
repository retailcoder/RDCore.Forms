# Drawing

A form and a PictureBox each have a drawing surface, a [`VBDrawingLayer`](../api/RDCore.Forms.Avalonia.Controls.VBDrawingLayer.yml): the destination of VB's drawing statements, `Line`, `Circle`, `PSet` and `Scale`, and of the `Cls`, `Point`, `Print`, `TextWidth` and `TextHeight` methods. A program reaches it through the [`IDrawingSurface`](../api/RDCore.Forms.Drawing.IDrawingSurface.yml) interface, which [`DrawingSurfaces.Of`](../api/RDCore.Forms.Drawing.DrawingSurfaces.yml) gives for a shown form's root or PictureBox. The interface is part of `RDCore.Forms` and has no Avalonia types.

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

The column and the line height are RDForms' own: the column is the OS/2 average width of the font file that draws the text, at its size, and the line is its ascent and descent together.

## Erasing

|Set|The drawing|`CurrentX`, `CurrentY`|
|---|---|---|
|`Picture`, to a picture or to none|Erased; the new picture shows.|Kept.|
|`BackColor` on a PictureBox|Erased, even where the colour is the same.|Kept.|
|`BackColor` on a form|Erased where the colour changes, kept where it is the same.|Kept.|

## Pens and fills

- Lines are drawn as a Line control draws them: `DrawWidth` pixels wide, dashed as `DrawStyle` says where they are one pixel wide, and solid where they are wider. Transparent draws no line.
- A box's or a circle's fill is its `FillColor` in its `FillStyle`: solid, or lines eight pixels apart from the surface's corner. An arc is not filled; a closed sector is. The fill stops where the edge starts.
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

## AutoRedraw and Paint

The surface keeps what it draws whatever the component's `AutoRedraw`, and keeps it as the component shrinks and grows again.

|`AutoRedraw`|Paint|
|---|---|
|False, the default|Raised once the surface is shown, and each time the component's inside grows. One is raised for the changes made before it comes, so a window grown across and down at once raises one.|
|True|Not raised.|

> [!NOTE]
> **Not implemented.** `PaintPicture` has no surface member.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

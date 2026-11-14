# Drawing

A form and a PictureBox each have a drawing surface, a [`VBDrawingLayer`](../api/RDCore.Forms.Avalonia.Controls.VBDrawingLayer.yml): the destination of VB's drawing statements, `Line`, `Circle`, `PSet` and `Scale`, and of the `Cls` and `Point` methods. A program reaches it through the [`IDrawingSurface`](../api/RDCore.Forms.Drawing.IDrawingSurface.yml) interface, which [`DrawingSurfaces.Of`](../api/RDCore.Forms.Drawing.DrawingSurfaces.yml) gives for a shown form's root or PictureBox. The interface is part of `RDCore.Forms` and has no Avalonia types.

```csharp
using RDCore.Forms.Drawing;

// Line (10, 10)-Step(100, 50), , B
var surface = DrawingSurfaces.Of(form.Root)!;
surface.Line(new GraphicsPoint(10, 10), new GraphicsPoint(100, 50, IsRelative: true), box: LineBox.Box);
```

## Where it draws

- The surface covers the component's inside: a form's client area, and a PictureBox's inside its border.
- What it draws is under every control, Labels, Images, Shapes and Lines included. On a PictureBox it is over the `Picture`.
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

## Statements and methods

|Member|Draws|`CurrentX`, `CurrentY` after|
|---|---|---|
|`Line`|A line from the first point, or from the current point, to the second. `B` draws a box, filled as `FillStyle` says; `BF` fills it with the line's colour. A box covers both corners.|The second point.|
|`Circle`|A circle, an ellipse (with an aspect), an arc (with a start and an end, in radians), or a sector. A negative start or end also draws a radius to that end, and with both negative the sector is closed. The radius is in horizontal units. An angle outside -2π to 2π is error 5.|The centre.|
|`PSet`|A point; a dot `DrawWidth` wide where that is more than 1.|The point.|
|`Scale`|Nothing. With two points it sets a user scale whose corners they are; with none it sets the scale back to twips.|Unchanged.|
|`Cls`|Clears the drawing.|0, 0.|
|`Point`|Nothing. It returns the colour at a point, the `BackColor` where nothing is drawn, and -1 outside the surface.|Unchanged.|

A point written with `Step` is measured from the current point, and a `Line`'s second point from its first. A colour left out is the component's `ForeColor`.

## Pens and fills

- Lines are drawn as a Line control draws them: `DrawWidth` pixels wide, dashed as `DrawStyle` says where they are one pixel wide, and solid where they are wider. Transparent draws no line.
- A box's or a circle's fill is its `FillColor` in its `FillStyle`: solid, or lines eight pixels apart from the surface's corner. An arc is not filled; a closed sector is.
- There is no smoothing.

## AutoRedraw and Paint

The surface keeps what it draws whatever the component's `AutoRedraw`, and keeps it as the component shrinks and grows again.

|`AutoRedraw`|Paint|
|---|---|
|False, the default|Raised once the surface is shown, and each time the component's inside grows. One is raised for the changes made before it comes, so a window grown across and down at once raises one.|
|True|Not raised.|

> [!NOTE]
> **Not implemented.** Every `DrawMode` draws as Copy Pen (13). `Point` reads the drawing and the `BackColor`; a PictureBox's `Picture` under the drawing is not read. The `Print` method, `PaintPicture`, `TextWidth` and `TextHeight` have no surface member.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

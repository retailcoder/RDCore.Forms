# Containers and pictures

Frame and PictureBox hold controls; PictureBox and Image draw pictures. The three controls' behaviour was measured with VB6.

## Frame

[`VBFrame`](../api/RDCore.Forms.Avalonia.Controls.VBFrame.yml)

- An etched edge runs through the middle of the caption's line, and the caption paints its own background over it.
- Its background covers the whole control.
- It takes no focus. It raises MouseUp before Click, and Click only where the release is over the Frame again. A press on a control it holds is that control's.

## PictureBox

[`VBPictureBox`](../api/RDCore.Forms.Avalonia.Controls.VBPictureBox.yml)

- Its border is a sunken edge when 3-D, and one pixel when flat. The controls it holds are placed inside the border, from its client origin.
- Its `Picture` is drawn at its own size in pixels from the client origin, clipped, under the controls it holds.
- Where `AutoSize` is set, it takes its picture's size: see [A size from the picture](#a-size-from-the-picture).
- Where `Align` is set, it is docked to an edge of its form: see [An aligned PictureBox](#an-aligned-picturebox).
- It raises Resize as its size changes: see [A PictureBox's Resize](#a-pictureboxs-resize).
- Its drawing statements draw over its `Picture` and under its controls; see [Drawing](drawing.md). It raises Paint as its drawing surface does.
- It takes the focus, whether or not it holds controls. It raises MouseUp before Click, and Click only where the release is over the PictureBox again. Its mouse events' X and Y are in twips.

Its key events are RDForms' own, not measured with VB6. With the focus, it raises KeyDown and KeyUp for every key, and KeyPress between them for a key that types a character, in the order of [A control's key events](form-events.md#a-controls-key-events). An arrow key raises its KeyDown and KeyUp and leaves the focus where it is. A key pressed in a control it holds is that control's alone.

## Image

[`VBImage`](../api/RDCore.Forms.Avalonia.Controls.VBImage.yml) is windowless: it takes no focus, and it is under every windowed control in its container.

- Its single border is a sunken edge.
- Its `Picture` is drawn inside its border: stretched over the whole of it where `Stretch` is set, and otherwise at its own size in pixels from the top left corner, clipped.
- Where `Stretch` is not set, it takes its picture's size: see [A size from the picture](#a-size-from-the-picture).
- The whole rectangle takes the pointer, where it draws nothing too. It raises MouseUp before Click, and Click only where the release is over the image again.

## A size from the picture

An Image whose `Stretch` is not set, and a PictureBox whose `AutoSize` is set, take the size of their `Picture` inside their border. A sunken border adds 2 pixels on each side, and a flat one 1.

|Case|Image, `Stretch` not set|PictureBox, `AutoSize` set|
|---|---|---|
|`Picture` set|Takes the picture's size. Its `Left` and `Top` go to whole pixels.|Takes the picture's size. Its `Left` and `Top` are kept.|
|A picture from the form's `.frx`|Takes the picture's size as the control is made, whatever size the form file holds.|The same.|
|`Picture` cleared|Keeps its size.|Keeps its size.|
|`Stretch` cleared, or `AutoSize` set, where there is a picture|Takes the picture's size.|Takes the picture's size.|
|`Stretch` set, or `AutoSize` cleared|Keeps its size.|Keeps its size.|
|`Width` or `Height` set by code|Kept, also when the same picture is set again.|Kept, also when the same picture is set again.|

## A PictureBox's Resize

A PictureBox raises Resize inside the statement that changes its size in whole pixels or its client area, as measured with VB6:

|Change|Resize|
|---|---|
|`Width` or `Height` set to a new size|One.|
|`Move` to a new size|One for both sizes.|
|`AutoSize` fitting a new picture size|One for both sizes.|
|`BorderStyle`, which changes the client area|One.|
|The same size, a change of less than a pixel, a position, `Visible`, `ScaleMode` or the scale|None.|
|Its form loading, an `AutoSize` fit to a picture from the `.frx` included|None.|

It raises Resize whether or not it or its form is shown.

## An aligned PictureBox

A PictureBox on the form itself whose `Align` is set is docked to that edge of the form's client area, as measured with VB6. [`AlignLayout`](../api/RDCore.Forms.Model.VB.AlignLayout.yml) lays the boxes out. A Data control and an `Alignable` [UserControl](usercontrols.md) are docked by the same rules.

- The top and bottom boxes are laid out first, each across the form's whole width. The left and right boxes then take the height between them.
- On each edge, the box nearest the edge is the last one in the form's file, whatever its `TabIndex`, and `ZOrder` does not change it. A box aligned at run time, or added by a program and aligned, goes inside the others.
- Each box keeps its own `Height`, or its `Width` for a left or right box, and takes that much room in whole pixels. Where the top and bottom boxes leave no height, the bottom ones go up past the top ones and the left and right boxes are 0 high.
- A hidden box takes no room and keeps its place. Shown again, it docks where it was among the others.
- The boxes follow the form's size, and each box's `Align`, `Visible` and size. A box whose `Align` is set to 0 keeps its last place and size.
- A box in a Frame or in another PictureBox is not aligned: its `Align` reads 0 whatever its file says, and a program's assignment leaves it so.
- On an MDI form, a box is aligned to the top unless its file says otherwise, and an assignment of 0 to its `Align` leaves it as it was.

A program's assignments to an aligned box, through [`VBRunTime.Assign`](../api/RDCore.Forms.Model.VB.VBRunTime.yml):

|Assignment|Top or bottom box|Left or right box|
|---|---|---|
|`Height`|Set. The boxes inside it move.|387, 'Height' property can't be set on this control|
|`Width`|387, 'Width' property can't be set on this control|Set. The boxes inside it move.|
|`Left`|387, 'Left' property can't be set on this control|No error; it stays where it is.|
|`Top`|No error; it stays where it is.|387, 'Top' property can't be set on this control|
|`Move` with a new size|Its height is set; the rest stays as it is.|Its width is set; the rest stays as it is (RDForms' own, not measured with VB6).|
|`Align` other than 0 to 4|380, Invalid property value|380, Invalid property value|

An aligned box raises Resize as its size changes, like any PictureBox, in this order:

|Change|Resize|
|---|---|
|Its form first shown|The form's, then each aligned box's, the top and bottom ones first, although no box's size changes.|
|The form's size|The form's, then each box whose size it changes.|
|A box's own height or width|Each other box whose size it changes, then its own.|

## Pictures

A `Picture` holds the bytes of a picture file. A picture from a VB6 form is read from its companion file (`.frx`), and a picture from code is the bytes of any file the formats below include.

|Format|Drawn|
|---|---|
|PNG|Yes, its transparent pixels showing what is behind them, and its half-transparent pixels blended over it, stretched or not.|
|BMP, JPEG|Yes.|
|GIF|Yes. Its transparent colour shows what is under the control: the form's `Picture` under an Image or a transparent Label, the `BackColor` of a PictureBox or an opaque Label. An animated GIF is not animated.|
|ICO, CUR|Yes. VB6 draws a cursor that is a form's `Picture` in black; RDForms draws it in its own colours.|
|WMF, EMF|No. A metafile is recognized and accepted, and not drawn.|

Before anything decodes a picture, [`FormPictures.Inspect`](../api/RDCore.Forms.Model.FormPictures.yml) reads its header with the RDCore SDK's `PictureHeader`, which checks every offset against the bytes. It refuses a picture:

- of more than 16 MiB;
- of a format it does not recognize, or whose header is cut short or holds an impossible value;
- wider or taller than 16,384 pixels, or of more than 16,777,216 pixels in all. An icon or a cursor is refused when any of its images is.

A picture whose header passes is drawn with the pixels the RDCore SDK's `PictureDecoder` decodes, alpha included, from the picture `LoadPicture` makes of the same bytes. A form draws the same pixels that `SavePicture` writes, and refuses no picture that `LoadPicture` loads and whose pixels the SDK decodes. An icon or a cursor draws the image `LoadPicture` loads when it is asked for no size: its 32 by 32 image of the most bits per pixel, or its first image where it has none of that size.

A refused picture is error 481, "Invalid picture". So is one that `LoadPicture` refuses, and one whose pixels the SDK does not decode. It is not drawn, the form still shows, and the control's `PictureError` holds the error. [`VBPicture.Decode`](../api/RDCore.Forms.Avalonia.Controls.VBPicture.yml) decodes a picture the same way for other uses.

The controls given the same bytes share one decode, as VB shares one picture object between the controls it is set on. The bytes are decoded once for any number of controls, and again only after none of them holds them.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

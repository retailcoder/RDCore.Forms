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

> [!NOTE]
> **Not implemented.** A PictureBox raises no Resize. VB6 raises one each time its size changes, by `AutoSize` or by code.

## Pictures

A `Picture` holds the bytes of a picture file. A picture from a VB6 form is read from its companion file (`.frx`), and a picture from code is the bytes of any file the formats below include.

|Format|Drawn|
|---|---|
|PNG|Yes, its transparent pixels showing what is behind them.|
|BMP, GIF, JPEG|Yes. An animated GIF is not animated.|
|ICO, CUR|Yes.|
|WMF, EMF|No. A metafile is recognized and accepted, and not drawn.|

Before anything decodes a picture, [`FormPictures.Inspect`](../api/RDCore.Forms.Model.FormPictures.yml) reads its header, with every offset checked against the bytes. It refuses a picture:

- of more than 16 MiB;
- of a format it does not recognize, or whose header is cut short or holds an impossible value;
- wider or taller than 16,384 pixels, or of more than 16,777,216 pixels in all. An icon or a cursor is refused when any of its images is.

A refused picture is error 481, "Invalid picture", and so is one whose header passes but whose body the decoder cannot read. It is not drawn, the form still shows, and the control's `PictureError` holds the error. [`VBPicture.Decode`](../api/RDCore.Forms.Avalonia.Controls.VBPicture.yml) decodes a picture the same way for other uses.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

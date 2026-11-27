# Containers and pictures

Frame and PictureBox hold controls; PictureBox and Image draw pictures. The three controls' behaviour was measured with VB6. A MultiPage holds pages of controls, one shown at a time, and a TabStrip shows tabs over an area that holds nothing: the two are MSForms classes, and their behaviour is RDForms' own, not measured with MSForms.

## Frame

[`RDFormsFrame`](../api/RDCore.Forms.Avalonia.Controls.RDFormsFrame.yml)

- An etched edge runs through the middle of the caption's line, and the caption paints its own background over it.
- Its background covers the whole control.
- It takes no focus. It raises MouseUp before Click, and Click only where the release is over the Frame again. A press on a control it holds is that control's.
- It scrolls the controls it holds: see [Scrolling a form or a Frame](#scrolling-a-form-or-a-frame).

## Scrolling a form or a Frame

A form and a Frame scroll the controls they hold. The members are MSForms', in pixels. What follows is RDForms' own, not measured with MSForms.

|Member|Default|What it does|
|---|---|---|
|`ScrollBars`|None|The bars it can show: None, Horizontal, Vertical or Both. A bar shows where there is more to scroll than the inside shows.|
|`KeepScrollBarsVisible`|Both|The bars of `ScrollBars` that show even with nothing to scroll.|
|`ScrollWidth`, `ScrollHeight`|0|The size of the area it scrolls. At 0, or at no more than the inside's size, it has nothing to scroll that way.|
|`ScrollLeft`, `ScrollTop`|0|How far it is scrolled. Setting one scrolls the inside, no further than the area's end, whether or not a bar shows, and the property keeps the value it was given. The user's scroll sets both.|

The mouse wheel scrolls a form or a Frame that has more to scroll than it shows, with or without a bar. A Frame's bars are at the edges of the whole Frame, over its edge.

Each scroll that moves the inside raises `Scroll(ActionX, ActionY, RequestDx, RequestDy, ActualDx, ActualDy)`: what scrolled it across and down, the distance asked, and the distance it scrolled, in pixels, to the right and down. A program's `ScrollLeft` or `ScrollTop` raises it at once; the user's scroll raises it once the scroll is done, with the distance asked the same as the distance scrolled.

|What scrolled it|Action|
|---|---|
|A program setting `ScrollLeft` or `ScrollTop`|PropertyChange (8), in that direction only. The distance asked is the change of the property.|
|A bar's arrow, an arrow key, or the mouse wheel|LineUp (1) or LineDown (2)|
|A bar's track, Page Up or Page Down|PageUp (3) or PageDown (4)|
|Home or End|Begin (5) or End (6)|
|A bar's thumb, dragged|AbsoluteChange (7)|
|A control taking the focus, scrolled into view|FocusRequest (10)|

> [!NOTE]
> **Not implemented.** The `Scroll` method, `VerticalScrollBarSide`, and the ControlRequest (9) action.

## PictureBox

[`RDFormsPictureBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsPictureBox.yml)

- Its border is a sunken edge when 3-D, and one pixel when flat. The controls it holds are placed inside the border, from its client origin.
- Its `Picture` is drawn at its own size in pixels from the client origin, clipped, under the controls it holds.
- Where `AutoSize` is set, it takes its picture's size: see [A size from the picture](#a-size-from-the-picture).
- Where `Align` is set, it is docked to an edge of its form: see [An aligned PictureBox](#an-aligned-picturebox).
- It raises Resize as its size changes: see [A PictureBox's Resize](#a-pictureboxs-resize).
- Its drawing statements draw over its `Picture` and under its controls; see [Drawing](drawing.md). It raises Paint as its drawing surface does.
- It takes the focus, whether or not it holds controls. It raises MouseUp before Click, and Click only where the release is over the PictureBox again. Its mouse events' X and Y are in twips.

Its key events are RDForms' own, not measured with VB6. With the focus, it raises KeyDown and KeyUp for every key, and KeyPress between them for a key that types a character, in the order of [A control's key events](form-events.md#a-controls-key-events). An arrow key raises its KeyDown and KeyUp and leaves the focus where it is. A key pressed in a control it holds is that control's alone.

## Image

[`RDFormsImage`](../api/RDCore.Forms.Avalonia.Controls.RDFormsImage.yml) is windowless: it takes no focus, and it is under every windowed control in its container.

- Its single border is a sunken edge.
- Its `Picture` is drawn inside its border: stretched over the whole of it where `Stretch` is set, and otherwise at its own size in pixels from the top left corner, clipped.
- Where `Stretch` is not set, it takes its picture's size: see [A size from the picture](#a-size-from-the-picture).
- The whole rectangle takes the pointer, where it draws nothing too. It raises MouseUp before Click, and Click only where the release is over the image again.

## MultiPage

[`RDFormsMultiPage`](../api/RDCore.Forms.Avalonia.Controls.RDFormsMultiPage.yml) shows a tab for each of its pages, and the selected page under them. Each page is a [`RDFormsPage`](../api/RDCore.Forms.Avalonia.Controls.RDFormsPage.yml).

- A page, of the class `RDForms.Page`, is placed only in a MultiPage, and a MultiPage holds only pages. A page has no place or size of its own: it fills the MultiPage under its tabs. The controls placed in a page are shown on it, as a Frame's are.
- A page's tab shows its `Caption`, with its access key, and its `ToolTipText`. A hidden page has no tab. A disabled page's tab is drawn disabled and cannot be selected.
- `Value` is the index of the selected page, from 0, or -1 where there is none. Only the selected page and its controls are shown. A program can set it to -1 even where there are pages. Any other value outside the pages is error 380: the selection stays, and no Change is raised.
- A press of the left button on an enabled tab selects its page, which raises Change where `Value` changes, and takes the focus. Its release over the same tab raises Click with the tab's index, after MouseUp.
- Its mouse events take the tab's index first, -1 where the pointer is over no tab: `MouseDown(Index, Button, Shift, X, Y)`. X and Y are in twips. A double click's second press raises `DblClick(Index, Cancel)` in place of MouseDown, and no Click.
- With the focus on the MultiPage itself, the arrow keys along its tabs select the next or the previous tab, skipping the hidden and the disabled ones, and stop at the last and the first. From anywhere in it, Ctrl+Tab and Ctrl+Shift+Tab do the same, and go round. Each raises Change and Click.
- A program changing `Value` raises Change once, and none where the value is the same.
- `Style` draws the tabs as tabs, as buttons, or not at all. `TabOrientation` puts them on the top, bottom, left or right edge. `MultiRow` wraps them on several rows where they do not fit on one. `TabFixedWidth` and `TabFixedHeight` give every tab that size in pixels, and 0 sizes each one to its caption.

[`Pages`](../api/RDCore.Forms.Model.Pages.yml) is a MultiPage's collection of pages, as a program's `Pages` is:

|Member|Behaviour|
|---|---|
|`Count`, `Item`|The pages, by index from 0 or by name without regard to case. An index outside them is 5, and a name no page has is 730.|
|`Add`|Adds a page, by default at the end, named `Page` and the first free number, and captioned with its name. An index outside 0 to `Count` is 5.|
|`Remove`, `Clear`|Remove pages, with the controls on them.|
|`SelectedItem`|The selected page, or none.|

`Value` follows the selected page as pages are added and removed. The first page added is selected. Where the selected page is removed, the page after it is selected, or the last page where it was the last.

A MultiPage added by a program gets two pages, named `Page` and the first free numbers.

## TabStrip

[`RDFormsTabStrip`](../api/RDCore.Forms.Avalonia.Controls.RDFormsTabStrip.yml) shows tabs over a client area. It holds no controls: a program shows what each tab stands for in controls placed over the client area.

- Its tabs are its `Tabs` list, two by default, `Tab1` and `Tab2`. Each tab has a `Name`, a `Caption`, a `ToolTipText`, `Enabled`, `Visible` and a `Tag`.
- `Value`, `Style`, `TabOrientation`, `MultiRow`, `TabFixedWidth` and `TabFixedHeight`, the mouse, the keys and the events are a MultiPage's, for its tabs.
- `ClientLeft`, `ClientTop`, `ClientWidth` and `ClientHeight` give the client area in pixels from the TabStrip's top left corner: the area inside its edge that the tabs leave, or the whole TabStrip inside its edge where `Style` draws no tabs.

[`Tabs`](../api/RDCore.Forms.Model.Tabs.yml) is a TabStrip's collection of tabs, as a program's `Tabs` is: `Count`, `Item` by index or name, `Add` (named `Tab` and the first free number by default), `Remove`, `Clear` and `SelectedItem`, as a MultiPage's `Pages` has. An index outside the tabs, a name no tab has, and a name another tab has are 5.

> [!NOTE]
> **Not implemented.** A page's or a tab's index cannot be changed. A page's `Picture` is not drawn, and a page has no scroll bars. A MultiPage declares Layout, AddControl and RemoveControl and does not raise them, and it has no Scroll event. The order of the events was not measured with MSForms.

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

A PictureBox on the form itself whose `Align` is set is docked to that edge of the form's client area, as measured with VB6. [`AlignLayout`](../api/RDCore.Forms.Model.AlignLayout.yml) lays the boxes out. A Data control and an `Alignable` [UserControl](usercontrols.md) are docked by the same rules.

- The top and bottom boxes are laid out first, each across the form's whole width. The left and right boxes then take the height between them.
- On each edge, the box nearest the edge is the last one in the form's file, whatever its `TabIndex`, and `ZOrder` does not change it. A box aligned at run time, or added by a program and aligned, goes inside the others.
- Each box keeps its own `Height`, or its `Width` for a left or right box, and takes that much room in whole pixels. Where the top and bottom boxes leave no height, the bottom ones go up past the top ones and the left and right boxes are 0 high.
- A hidden box takes no room and keeps its place. Shown again, it docks where it was among the others.
- The boxes follow the form's size, and each box's `Align`, `Visible` and size. A box whose `Align` is set to 0 keeps its last place and size.
- A box in a Frame or in another PictureBox is not aligned: its `Align` reads 0 whatever its file says, and a program's assignment leaves it so.
- On an MDI form, a box is aligned to the top unless its file says otherwise, and an assignment of 0 to its `Align` leaves it as it was.

A program's assignments to an aligned box, through [`RunTime.Assign`](../api/RDCore.Forms.Model.RunTime.yml):

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

A refused picture is error 481, "Invalid picture". So is one that `LoadPicture` refuses, and one whose pixels the SDK does not decode. It is not drawn, the form still shows, and the control's `PictureError` holds the error. [`RDFormsPicture.Decode`](../api/RDCore.Forms.Avalonia.Controls.RDFormsPicture.yml) decodes a picture the same way for other uses.

The controls given the same bytes share one decode, as VB shares one picture object between the controls it is set on. The bytes are decoded once for any number of controls, and again only after none of them holds them.

## Converting from MSForms

The names an MSForms Frame, MultiPage, Page and TabStrip take in RDForms (see [Converting from MSForms](index.md#converting-from-msforms)):

|MSForms|RDForms|
|---|---|
|`InsideWidth` and `InsideHeight` of a Frame or a Page|`ScaleWidth` and `ScaleHeight`, in pixels.|
|`ScrollLeft`, `ScrollTop`, `ScrollWidth` and `ScrollHeight` of a form, a Frame or a Page, in points|The same, in pixels.|
|`TabFixedWidth` and `TabFixedHeight` of a MultiPage or a TabStrip, in points|The same, in pixels.|
|`ClientLeft`, `ClientTop`, `ClientWidth` and `ClientHeight` of a TabStrip, in points|The same, in pixels.|
|`Accelerator` of a Page or a Tab|A `&` before the access key's letter in `Caption`. An assignment of `Accelerator` from code is RDC40001.|
|Scroll, AddControl and RemoveControl of a form or a Frame|The same. Scroll's `ActionX` and `ActionY` are `RDForms.ScrollAction`, and its `ActualDx` and `ActualDy` ByRef `Single`. A MultiPage's take the page's `Index` first.|

> [!NOTE]
> **Not implemented.** A Frame and a Page have no `Zoom` and no clipboard and undo members (`Copy`, `Cut`, `Paste`, `UndoAction`, `RedoAction`, `CanPaste`, `CanUndo` and `CanRedo`), and a Frame and a MultiPage raise no Zoom event. The BeforeDragOver, BeforeDropOrPaste and Error of a Frame, a MultiPage, a TabStrip and an Image are declared and never raised.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

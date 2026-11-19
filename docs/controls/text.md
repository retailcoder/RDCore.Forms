# Text

Label and TextBox. The behaviour below was measured with VB6.

## Label

[`RDFormsLabel`](../api/RDCore.Forms.Avalonia.Controls.RDFormsLabel.yml) is windowless: it takes no focus, and it is under every windowed control in its container.

|Property|Behaviour|
|---|---|
|`Caption`, `UseMnemonic`|Where `UseMnemonic` is set, a single `&` marks an access key; otherwise every `&` is drawn.|
|`Alignment`, `WordWrap`|Align the caption. It wraps at the label's width, except where `AutoSize` is set and `WordWrap` is not: that caption stays on one line.|
|`BackStyle`|Transparent paints no background.|
|`BorderStyle`, `Appearance`|A single border is a sunken edge when 3-D, and one pixel when flat.|
|`Picture`, `PicturePosition`|RD-VBA addition: a picture laid out against the caption, as MSForms' Label lays it out. See [Picture](#picture).|

It raises MouseUp before Click, and Click only where the release is over the label again.

### Picture

A Label's `Picture` and `PicturePosition` are an RD-VBA addition: VB6's Label has neither. They follow MSForms' Label, as measured in Excel.

- The picture and the caption make one block, centred in the label. A block larger than the label starts at its edge.
- `PicturePosition` takes MSForms' values, 0 to 12. The first word places the picture, the second aligns the caption with it. The default is 7, `AboveCenter`.

|`PicturePosition`|Layout|
|---|---|
|0 to 5, `LeftTop` to `RightBottom`|The picture beside the caption, one pixel from it, their tops, centres or bottoms aligned.|
|6 to 11, `AboveLeft` to `BelowRight`|The picture above or below the caption, one pixel from it, their left edges, centres or right edges aligned.|
|12, `Center`|The caption over the picture, both centred.|

- `Alignment` aligns the caption's lines within the block and leaves the block where it is.
- A picture larger than the room it has is shrunk on that axis alone. Beside the caption, its width leaves the caption room; above or below the caption, its height does not, and the caption is clipped.
- The colour of a BMP's lower-left pixel is transparent, so a BMP of one colour draws nothing. A GIF keeps its own transparent colour and is drawn whole otherwise. RDForms also draws a JPEG, a PNG or an icon as it is.
- The space between picture and caption follows RDForms' text measurement, so the caption can be a pixel from where MSForms puts it.

> [!NOTE]
> **Not implemented.** `AutoSize` does not fit a Label to its caption or to its picture.

## TextBox

[`RDFormsTextBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsTextBox.yml)

|Case|Behaviour|
|---|---|
|`Text`|Typing sets it, and a change from code shows in the box.|
|Change|Raised from the component's `Text`, whichever way it changed, and only when it changed.|
|A character typed|A KeyPress first, with its `KeyAscii`. A handler that sets it to 0 cancels the character; one that sets another code types that character instead. Backspace is 8, and Ctrl+A is 1.|
|The form's `KeyPreview`|True raises the form's KeyDown, KeyPress and KeyUp before the box's: see [Form events](form-events.md#keyboard).|
|`Locked`|The text cannot be edited.|
|`Alignment`|Aligns the text left, right or centre.|
|`BorderStyle`|None draws no border.|
|The mouse|MouseUp comes before Click.|
|`MultiLine`|A multi-line box types a new line, CR LF, for Enter and for Ctrl+Enter, after their KeyPress (13 and 10). A single-line box types nothing for either, and shows a CR LF its `Text` holds as it is.|
|`ScrollBars`|A multi-line box wraps its lines unless it has a horizontal scroll bar. A single-line box has no scroll bar, whatever its `ScrollBars`.|
|`MultiLine` and `ScrollBars` from code|On a Visual Basic 6.0 form, only a designer and a form's file set them: VB refuses a program's late-bound assignment with 382, and early-bound code that assigns them does not compile. On an RDForms form, a program sets them, as Microsoft Forms 2.0 lets it, and the box shows them as a box made with them does.|
|`PasswordChar`|One character, or "" for none; a longer value is 380. The box shows that character in place of each character of its text, in a multi-line box too, and `Text` is the text typed.|
|`MaxLength`|0 to 65535, with 0 for no limit; another value is 380. A character typed into a full box is a KeyPress, and changes nothing. A `Text` set from code is cut to the limit, with one Change.|
|A text longer than `MaxLength`|A lower `MaxLength` keeps it, and so does a form's file that holds it, until the `Text` is set again.|
|A default button|In a multi-line box, the form's default button takes Enter, as in a single-line one, and Ctrl+Enter types a new line (RDForms' own).|

### Selection

`SelStart`, `SelLength` and `SelText` are the box's selection. A program reads and sets them on a loaded form as on a shown one, and reads back a selection the user makes. The component holds them, with or without a control to show them, and only a running program sets them ([`PropertyDefinition.RunTimeOnly`](../api/RDCore.Forms.Model.PropertyDefinition.yml)): a form's file does not hold them.

|Member|Behaviour|
|---|---|
|`SelStart`|Where the selection starts, or the caret's place: 0 is before the first character. It takes 0 to 65535, a value past the end of the text being its end; another value is 380. Setting it selects nothing.|
|`SelLength`|The number of characters selected from `SelStart`. It takes 0 to 65535, a value past the end of the text selecting up to it; another value is 380.|
|`SelText`|The selected text, "" where nothing is selected. Setting it puts the text in place of the selection, or at the caret, and leaves the caret after it with nothing selected. Where the box has a `MaxLength`, what does not fit is left out. A `Locked` box takes it too.|
|Change from `SelText`|A single-line box raises one Change, even where the text does not change, and none where nothing is selected and a text other than "" does not fit at all. A multi-line box raises one Change as it deletes the selection, where there is one, and one as it inserts the text, where some of it fits.|
|`Text` from code|A different text selects nothing, the caret before the first character. The same text keeps the selection, with no Change.|
|A new line|`SelStart` and `SelLength` count its CR and its LF, and a selection can start or end between them. The box shows the caret at the nearest place it can.|

## Converting from MSForms

The names an MSForms Label and TextBox take in RDForms (see [Converting from MSForms](index.md#converting-from-msforms)):

|MSForms|RDForms|
|---|---|
|`Accelerator` of a Label|A `&` before the access key's letter in `Caption`. An assignment of `Accelerator` from code is RDC40001.|
|`TextAlign` of a Label or a TextBox|`Alignment`, with VB's values: `fmTextAlignLeft` (1) is 0, `fmTextAlignCenter` (2) is 2, and `fmTextAlignRight` (3) is 1.|
|`Value` of a TextBox|`Text`.|
|`BorderStyle`, `fmBorderStyleNone` and `fmBorderStyleSingle`|`BorderStyle`, 0 and 1.|
|`MultiLine`, `SelStart`, `SelLength`, `SelText`, `PasswordChar` and the other members of the same name|The same.|

> [!NOTE]
> **Not implemented.** A TextBox has no `IMEMode` and no `Copy`, `Cut`, `Paste` or `CanPaste`. A Label's and a TextBox's BeforeDragOver, BeforeDropOrPaste and Error are declared and never raised.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

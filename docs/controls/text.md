# Text

Label and TextBox. The behaviour below was measured with VB6.

## Label

[`VBLabel`](../api/RDCore.Forms.Avalonia.Controls.VBLabel.yml) is windowless: it takes no focus, and it is under every windowed control in its container.

|Property|Behaviour|
|---|---|
|`Caption`, `UseMnemonic`|Where `UseMnemonic` is set, a single `&` marks an access key; otherwise every `&` is drawn.|
|`Alignment`, `WordWrap`|Align and wrap the caption.|
|`BackStyle`|Transparent paints no background.|
|`BorderStyle`, `Appearance`|A single border is a sunken edge when 3-D, and one pixel when flat.|

It raises MouseUp before Click, and Click only where the release is over the label again.

## TextBox

[`VBTextBox`](../api/RDCore.Forms.Avalonia.Controls.VBTextBox.yml)

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

### MultiLine and ScrollBars

Both are set at design time. A box keeps the ones it was made with.

|Box|Behaviour|
|---|---|
|Single-line|One line, scrolled across as the caret moves. `ScrollBars` shows no bar. Enter raises KeyDown, KeyPress 13 and KeyUp, and types nothing.|
|MultiLine, no horizontal bar|The text wraps at the box's edge.|
|MultiLine, a horizontal bar|The text does not wrap.|
|MultiLine, a bar asked for|Shown, with nothing to scroll too.|
|Enter in a MultiLine box|KeyDown, KeyPress 13, a line break (CR LF), then KeyUp. Where the form has a default button, the button takes Enter as it does from any control.|
|Ctrl+Enter in a MultiLine box|KeyDown with `Shift` 2, KeyPress 10, then a line break. No button takes it.|
|Tab in a MultiLine box|The focus moves to the next control. No tab is typed.|

A `KeyAscii` of 0 for Enter breaks no line, and another code types that character instead. This rule is RDForms' own, not measured with VB6.

### PasswordChar and MaxLength

These rules are RDForms' own, not measured with VB6.

- `PasswordChar` shows its first character in place of each character of the text. An empty `PasswordChar` shows the text, and a MultiLine box shows its text whatever its `PasswordChar`.
- `MaxLength` limits what the user types, and 0 sets no limit. A key past the limit still raises its KeyPress, and no Change. A text set from code is kept whole.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

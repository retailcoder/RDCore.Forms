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

> [!NOTE]
> **Not implemented.** `MultiLine`, `ScrollBars`, `PasswordChar` and `MaxLength` are not in the model, so a TextBox is one line, shows its text, and takes text of any length.

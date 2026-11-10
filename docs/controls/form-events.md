# Form events

A [`VBFormWindow`](../api/RDCore.Forms.Avalonia.Rendering.VBFormWindow.yml) raises its root's own events to the window's sink, in the order VB6 raises them, as measured with native and p-code builds.

Initialize, Load and Terminate are the program's: the window raises none of them. A program raises Initialize and Load before it shows the window.

## Showing and resizing

|Situation|Raised|
|---|---|
|The window is shown for the first time|Resize, inside `Show`, then Activate.|
|The root's `Width` or `Height` changes|One Resize for each change, inside the assignment. A size the root already has raises nothing.|
|The user resizes the window|The root's `Width` and `Height` take the client area's size, in twips, and one Resize is raised for both.|
|The window is shown again after `Hide`|No Resize.|

A Resize is raised for a root whose window was shown, hidden or not. With `AutoRedraw` False, Paint follows Resize as the window is shown and as its client area grows: see [Drawing](drawing.md).

## Activation

|Situation|Raised|
|---|---|
|Another of the program's form windows becomes active|Deactivate on the one that was active, then Activate on the new one.|
|Another application takes the activation and gives it back|Nothing.|
|A window whose root is disabled is shown|No Activate: it is shown without being activated.|

## Unloading

|Situation|Raised|
|---|---|
|The user clicks the close box|QueryUnload with an `UnloadMode` of 0, then Unload.|
|[`Unload`](../api/RDCore.Forms.Avalonia.Rendering.VBFormWindow.yml) is called, as VB's `Unload` statement|QueryUnload with an `UnloadMode` of 1, then Unload. It returns whether the form unloaded.|
|`Close` is called, as VB's `End` closes a form|Nothing.|
|`Unload` is called from the root's own QueryUnload or Unload|Nothing: it returns False and the unload goes on.|

A handler that sets `Cancel` to a value other than 0, in QueryUnload or in Unload, keeps the window open. A window closed for its owner closing raises them with an `UnloadMode` of 5, and one closed as Windows ends its session with 2.

## Mouse

A press on none of the form's controls is the root's.

- It raises MouseDown, MouseMove and MouseUp. `X` and `Y` are in the root's scale: its `ScaleMode`, from its `ScaleLeft` and `ScaleTop`.
- Click comes after MouseUp, for the right and middle buttons too, where the release is over the form again, wherever the press was.
- A release over a control, or outside the window, raises the root's MouseUp and no Click.

> [!NOTE]
> **Not implemented.** The root's DblClick: a double click on the form is two clicks.

## Keyboard

|Focus|Raised on the root|
|---|---|
|On none of the form's controls|KeyDown, KeyPress and KeyUp, for every key.|
|On a control, with the root's `KeyPreview` False|Nothing.|
|On a control, with the root's `KeyPreview` True|Each key event, before the control's.|

With `KeyPreview` True, the root's handlers decide what the control raises:

- A `KeyCode` the root's KeyDown sets to 0 raises no KeyDown on the control, and the key still does what it does. Another code is not passed on.
- The control's KeyPress has the `KeyAscii` the root's KeyPress left, and 0 raises none and types nothing.
- The control's KeyUp has the key's own code.

The root's KeyPress follows the TextBox's rules: a character outside ASCII raises none, Backspace is 8 and Ctrl+A is 1.

## A control's key events

The TextBox, CommandButton, CheckBox, OptionButton, ListBox and ComboBox raise their own KeyDown, KeyPress and KeyUp, in the order measured with VB6: KeyDown, KeyPress for a key that types a character, then what the key does, then KeyUp. With `KeyPreview` True, each of the root's events comes just before the control's.

|Key|Raised|
|---|---|
|Space on a CommandButton or a CheckBox|KeyDown, KeyPress 32 and KeyUp, then Click.|
|Space on the selected OptionButton|KeyDown, KeyPress 32 and KeyUp, and no Click.|
|An arrow on a CommandButton, a CheckBox or an OptionButton|No key event, the root's included: the focus moves. A CommandButton, CheckBox or OptionButton that takes it raises no KeyUp; another control raises its KeyUp alone.|
|An arrow in a ListBox or a ComboBox|KeyDown, the Click of the selection, then KeyUp.|
|A character in a ListBox or a ComboBox|KeyDown, KeyPress, the Click of the selection or the Change of the text, then KeyUp.|

A control's KeyPress follows the TextBox's rules: a `KeyAscii` of 0 types nothing, and another code is taken in place of the character.

A character typed in a ListBox whose `MultiSelect` is None selects the next item that starts with it, after the selected one and from the top again past the last, without regard to case.

> [!NOTE]
> **Not implemented.** The root's GotFocus and LostFocus. Enter and Esc raise no key events, and a CommandButton has no `Default` or `Cancel`. A scroll bar and a PictureBox raise no key events of their own: with `KeyPreview` True, a key on one of them raises the root's events alone.

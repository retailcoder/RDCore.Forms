# Buttons

CommandButton, CheckBox, OptionButton and ToggleButton. Each draws its `Caption` with its access key: a single `&` marks the character after it, and `&&` shows one `&`.

The behaviour of the CommandButton, the CheckBox and the OptionButton was measured with VB6. VB6 has no ToggleButton, which comes from MSForms: see [ToggleButton](#togglebutton).

## CommandButton

[`RDFormsCommandButton`](../api/RDCore.Forms.Avalonia.Controls.RDFormsCommandButton.yml)

- Click comes before MouseUp.
- An arrow key moves the focus to the previous or next control in its container, and raises no key event.
- Space raises KeyDown, KeyPress and KeyUp, then Click: see [A control's key events](form-events.md#a-controls-key-events).
- Enter on the button raises its Click and no key event.
- `Default` True makes it the button Enter clicks, and `Cancel` True the button Esc clicks, from any control of the form: see [Enter and Esc](form-events.md#enter-and-esc).
- The button Enter clicks has the default look: the default button while no CommandButton has the focus, and the focused CommandButton otherwise.
- `Value` True from code raises Click before the statement returns, with no focus or mouse event, even where the button is disabled or hidden, or its form is disabled, hidden, or loaded and never shown. Click reads `Value` True, and it is False again after. `Value` False does nothing. Setting `Value` True in Click raises Click again, inside the first.

These are RDForms' own, not measured with VB6:

- The look is a border two pixels wide in the system's `Highlight` colour, set on the button itself, so a style does not override it.
- The button with the look has the `:default` pseudo-class, which a host's style can select.
- A default button that is hidden or disabled takes no Enter, so it has no look.

A Standard CommandButton does not paint its `BackColor`: its face is the system's `ButtonFace` colour, pressed or not, in the palette of the form's theme variant (see [Theme](index.md#theme)). See [Style Graphical](#style-graphical) for its `Picture`.

A CommandButton has no `ForeColor`, as in VB6, where a program's read or assignment of it is 438. Its caption is drawn in the system's `ButtonText` colour. A VB6 form file or a form document that gives it a `ForeColor` keeps that value as an `UnmodelledProperty` (see [Format](../format/index.md)).

## CheckBox

[`RDFormsCheckBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsCheckBox.yml)

|Case|Behaviour|
|---|---|
|A click|Clears a checked or grayed box, and checks a cleared one.|
|`Value` from code|Shows in the box.|
|Click|Raised when `Value` changes, by a click or by code, and only then. It comes before MouseUp.|
|An arrow key|Moves the focus to the previous or next control in its container, and changes no `Value`.|
|The pointer|The whole rectangle takes it, whatever part of it the theme draws.|

## OptionButton

[`RDFormsOptionButton`](../api/RDCore.Forms.Avalonia.Controls.RDFormsOptionButton.yml)

The option buttons in one container are a group: selecting one clears the others.

|Case|Behaviour|
|---|---|
|`Value` set to False from code on the selected button|The group is left with none selected.|
|Click|Raised as a button becomes selected: by a click, by the focus, or by code. Clearing a button, or selecting the selected one again, raises none.|
|The focus, by the keyboard or from code|Selects the button before its GotFocus. Its Click comes after the GotFocus.|
|Tab|A group is one tab stop: its selected button, or each button where none is selected.|
|An arrow key|Moves the focus to the previous or next control in its container, and selects a button it lands on.|
|A double click|The second press raises MouseDown, then DblClick.|
|The pointer|The whole rectangle takes it, whatever part of it the theme draws.|

## ToggleButton

[`RDFormsToggleButton`](../api/RDCore.Forms.Avalonia.Controls.RDFormsToggleButton.yml)

A push button that stays down once clicked. Its `Value` is True while it is down, False while it is up, and Null where it is neither; a Null button is drawn up.

|Case|Behaviour|
|---|---|
|A click|Puts an up button down, and a down one up. With `TripleState` True, a click on a down button makes it Null, and a click on a Null one puts it up.|
|`Value` from code|Shows in the button. Null comes only from code or the form's file where `TripleState` is False.|
|Change, then Click|Raised when `Value` changes, by a click or by code, and only then. Both come before MouseUp.|
|`Locked` True|A click leaves `Value` as it is, and raises no Change or Click.|
|A double click|The second press raises MouseDown, then DblClick.|
|An arrow key|Moves the focus to the previous or next control in its container.|
|Space|Raises KeyDown, KeyPress and KeyUp, then clicks the button.|

Its behaviour is RDForms' own, not measured with MSForms.

> [!NOTE]
> **Not implemented.** The button does not draw its `Picture`, and ignores `PicturePosition`, `AutoSize`, `WordWrap`, `BackStyle` and `MouseIcon`. The form's file keeps their values.

## Style Graphical

A CommandButton, a CheckBox and an OptionButton have a `Style`: Standard, the default, or Graphical. It is set at design time; RDForms reads it as the control is made and ignores a later change.

|Style|Look|
|---|---|
|Standard|The control shows its caption only. Its `Picture` is not drawn.|
|Graphical|A push button, in its `BackColor`. The caption is at the bottom and the `Picture` is centred above it, or centred in the button where there is no caption. A CheckBox or an OptionButton keeps its behaviour, and is drawn as a push button.|

A Graphical button is down while it is pressed, and a Graphical CheckBox or OptionButton is also down while it is checked or selected. A grayed CheckBox is drawn as a cleared one.

|State|Picture|
|---|---|
|Up|`Picture`|
|Down|`DownPicture` where there is one, otherwise `Picture`. The picture and the caption are drawn one pixel right and down.|
|Disabled|`DisabledPicture` where there is one, otherwise `Picture`, greyed. The caption is greyed.|

A picture is drawn at its size in pixels. A greyed `Picture` or caption is drawn at half its opacity, and a disabled button keeps its `BackColor`: this look is RDForms' own, not measured with VB6.

### MaskColor

Where `UseMaskColor` is True, a picture's pixels of the `MaskColor` show the button's face, in its `BackColor`. By default `MaskColor` is silver (`&H00C0C0C0&`) and `UseMaskColor` is False.

|Case|Behaviour|
|---|---|
|A pixel of a colour near the `MaskColor`|Drawn: only the exact colour is left out.|
|A system colour as the `MaskColor`|The colour it stands for is left out.|
|`DownPicture` and `DisabledPicture`|Masked as the `Picture` is.|
|A bitmap, a GIF or a JPEG|Masked.|
|An icon or a cursor|Drawn whole, whatever `UseMaskColor` says.|
|`MaskColor` or `UseMaskColor` set at run time|The button is drawn again with the new value.|

These are RDForms' own, not measured with VB6:

- A PNG, which VB6 does not load, is masked as a bitmap: only its opaque pixels of the `MaskColor` are left out, and a pixel of that colour that is half transparent is blended over the face.
- A `Picture` greyed while the button is disabled keeps its masked pixels out: they show the face.

## Converting from MSForms

The names an MSForms button takes in RDForms (see [Converting from MSForms](index.md#converting-from-msforms)):

|MSForms|RDForms|
|---|---|
|`Accelerator` of a CommandButton, a CheckBox, an OptionButton or a ToggleButton|A `&` before the access key's letter in `Caption`. An assignment of `Accelerator` from code is RDC40001.|
|`Alignment` of a CheckBox, an OptionButton or a ToggleButton|`Alignment`, with VB's values: `fmAlignmentLeft` (0) is 1, and `fmAlignmentRight` (1) is 0. MSForms names the caption's side, VB the box's.|
|`Value`, `TripleState`, `Locked`, `Picture`, `PicturePosition` and the other members of the same name|The same.|

> [!NOTE]
> **Not implemented.** A button's BeforeDragOver, BeforeDropOrPaste and Error are declared and never raised.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

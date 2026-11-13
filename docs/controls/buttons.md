# Buttons

CommandButton, CheckBox and OptionButton. Each draws its `Caption` with its access key: a single `&` marks the character after it, and `&&` shows one `&`.

The behaviour below was measured with VB6.

## CommandButton

[`VBCommandButton`](../api/RDCore.Forms.Avalonia.Controls.VBCommandButton.yml)

- Click comes before MouseUp.
- An arrow key moves the focus to the previous or next control in its container, and raises no key event.
- Space raises KeyDown, KeyPress and KeyUp, then Click: see [A control's key events](form-events.md#a-controls-key-events).
- Enter on the button raises its Click and no key event.
- `Default` True makes it the button Enter clicks, and `Cancel` True the button Esc clicks, from any control of the form: see [Enter and Esc](form-events.md#enter-and-esc).
- The button Enter clicks has the default look: the default button while no CommandButton has the focus, and the focused CommandButton otherwise.

These are RDForms' own, not measured with VB6:

- The look is a border two pixels wide in the system's `Highlight` colour, set on the button itself, so a style does not override it.
- The button with the look has the `:default` pseudo-class, which a host's style can select.
- A default button that is hidden or disabled takes no Enter, so it has no look.

A Standard CommandButton does not paint its `BackColor`. See [Style Graphical](#style-graphical) for its `Picture`.

## CheckBox

[`VBCheckBox`](../api/RDCore.Forms.Avalonia.Controls.VBCheckBox.yml)

|Case|Behaviour|
|---|---|
|A click|Clears a checked or grayed box, and checks a cleared one.|
|`Value` from code|Shows in the box.|
|Click|Raised when `Value` changes, by a click or by code, and only then. It comes before MouseUp.|
|An arrow key|Moves the focus to the previous or next control in its container, and changes no `Value`.|
|The pointer|The whole rectangle takes it, whatever part of it the theme draws.|

## OptionButton

[`VBOptionButton`](../api/RDCore.Forms.Avalonia.Controls.VBOptionButton.yml)

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

> [!NOTE]
> **Not implemented.** `MaskColor` and `UseMaskColor` are not read, so a picture's background is drawn.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

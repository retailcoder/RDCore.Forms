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

Two rules here are RDForms' own, not measured with VB6. The look is a border two pixels wide in the system's `Highlight` colour, and the button has the `:default` pseudo-class, which a host's style can select. A default button that is hidden or disabled takes no Enter, so it has no look.

> [!NOTE]
> **Not implemented.** A CommandButton's `Picture` is not drawn.

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

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

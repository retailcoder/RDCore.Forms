# Data control

A Data control shows four buttons that move through a database's records, with its `Caption` between them. [`VBData`](../api/RDCore.Forms.Avalonia.Controls.VBData.yml) draws it as VB6 does. The look, the defaults, the ranges and the errors below were measured with VB6.

> [!NOTE]
> **Not implemented.** The control opens no database. Its buttons move through no records, and it raises no Validate, Reposition or Error. No control is bound to it.

## Look

|Part|Drawn|
|---|---|
|Edge|3-D: a ring of one pixel in the button face's colour, then a sunken edge two pixels deep. Flat: a border of one pixel in the window frame's colour.|
|Buttons|19 pixels wide, as tall as the control inside its edge: first and previous at the left, next and last at the right, each with its arrow glyph in the button text's colour.|
|Caption|On a strip in `BackColor` between the second and the third button, in `ForeColor`, or in the grey text colour where the control is disabled. The text starts three pixels into the strip, is centred down it, and is cut off at its end.|
|Narrow control|The caption's strip is the width left over after the four buttons. Where there is none, the last two buttons are drawn over the first two.|

The buttons keep their colours whatever `BackColor`, `ForeColor` and `Enabled` are.

> [!NOTE]
> **Not implemented.** VB6 draws a Data control less than 23 pixels tall as 23 pixels tall, though its `Height` reads back as set. `Align` does not dock the control to an edge of its form. The buttons are not drawn pressed, and the control raises no mouse event, Resize, drag event or OLE event.

## Properties

|Property|Default|Values|
|---|---|---|
|`Caption`|""|Any text. It is the control's default member: `Data1 = "x"` sets it.|
|`BackColor`, `ForeColor`|The window's colours: `&H80000005&` and `&H80000008&`.||
|`Connect`|`Access`|Any text.|
|`DatabaseName`, `RecordSource`|""|Any text.|
|`RecordsetType`|1, Dynaset|0 to 2. Another value is 380.|
|`DefaultType`|2, Jet|1 and 2. Another value is 380.|
|`DefaultCursorType`|0|0 to 2, and a value that names none, such as 3, as VB6 takes it.|
|`BOFAction`|0, move to the first record|0 and 1. Another value is 380.|
|`EOFAction`|0, move to the last record|0 to 2. Another value is 380.|
|`Exclusive`, `ReadOnly`|False||
|`Options`|0|A sum of DAO's recordset options.|
|`Negotiate`|False|A program can neither read it (393) nor set it (382): see [Properties set only at design time](index.md#properties-set-only-at-design-time).|

A Data control has no `TabIndex`, `TabStop`, `BorderStyle`, `BackStyle`, `HelpContextID` or `hWnd`, and takes no focus: reading one, or calling `SetFocus`, is 438 in VB6.

## Members a running program uses

|Member|Behaviour|Errors|
|---|---|---|
|`Database`, `Recordset`|Nothing.|Setting `Database` is 383.|
|`EditMode`|0.|Setting it is 383.|
|`Refresh`|Draws the control again.||
|`UpdateRecord`, `UpdateControls`||91: the control has no recordset.|

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

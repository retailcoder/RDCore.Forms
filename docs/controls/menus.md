# Menus

A form's menus are its `VB.Menu` components. [`FormMenu.Of`](../api/RDCore.Forms.Menus.FormMenu.yml) gives them for a form's root, as the items of its menu bar, each holding the items of its own menu. Each item is a [`FormMenuItem`](../api/RDCore.Forms.Menus.FormMenuItem.yml): a menu component with VB's members and the rules VB6 has for them. The model has no Avalonia types.

A [`VBFormWindow`](../api/RDCore.Forms.Avalonia.Rendering.VBFormWindow.yml) shows them on its [`VBMenuBar`](../api/RDCore.Forms.Avalonia.Rendering.VBMenuBar.yml), above the client area. The client area keeps the root's `Width` and `Height` whether or not the bar is shown.

```csharp
using RDCore.Forms.Menus;

var menu = FormMenu.Of(form.Root);
var save = menu.AllItems.Single(item => item.Component.Name == "mnuFileSave");
save.Shortcut = new MenuShortcut(83, ShortcutModifiers.Ctrl); // Ctrl+S
save.Enabled = false;
```

## The bar and its items

- The bar holds the root's menus, in the order the form holds them. An item that holds items opens a menu of them.
- An item whose `Caption` is a hyphen is a separator, drawn as a line. An ampersand in a caption marks the character after it as the item's access key, and two ampersands show one.
- Each item follows its component's `Caption`, `Enabled`, `Checked`, `Visible` and `Shortcut` as they change. A shortcut's key is shown beside its item.
- The bar is shown while one of its items is visible. A form whose menus are all hidden, as one that keeps them only to pop up, shows none.

## Click

The order is VB6's, as measured with native and p-code builds.

|Situation|Raised|
|---|---|
|A menu that holds items opens|Its Click, as it opens. Keeping it open, or closing it, raises nothing more.|
|An item that holds none is chosen|Its Click, alone: the menus above it raise none.|
|An item's shortcut is pressed|Each menu's Click from the bar down, then the item's.|

A shortcut stops at the first item on its way that is hidden or disabled. Each is asked after the Click above it has run, so a menu's Click can enable the item below it in time.

A shortcut chooses its item before the control that has the focus or the form sees the key, a TextBox included. Where two items have the same shortcut, the first in the form's order has it.

Choosing an item leaves its `Checked` as the program set it.

## Shortcuts

A [`MenuShortcut`](../api/RDCore.Forms.Menus.MenuShortcut.yml) is a key, by its VB `KeyCode`, with Shift, Ctrl and Alt. Its text is VB's: `^` for Ctrl, `+` for Shift and `%` for Alt, then a letter, a digit or a key name in braces, as in `^Q`, `{F2}` and `+^{DEL}`. The key names are the ones VB's `SendKeys` statement takes. A designer file's `Shortcut` is read this way, and one that is not a shortcut is kept as written, with a diagnostic.

## What VB6 refuses

[`FormMenuItem`](../api/RDCore.Forms.Menus.FormMenuItem.yml) refuses what VB6 refuses, with VB's error.

|Assignment|Error|
|---|---|
|`Checked` on a top-level item or a separator|387, 'Checked' property can't be set on this control|
|`Enabled` False on a separator|387, 'Enabled' property can't be set on this control|
|`Visible` False on the last visible item of a menu, a separator included|387, 'Visible' property can't be set on this control|
|A `Caption` of `-` on an item that is not a separator|395, Cannot use separator bar as menu name for this control|

The last visible item of the bar can be hidden, and the bar goes with it.

## RD-VBA additions

|Member|Behaviour|
|---|---|
|`Shortcut`|Any key of the names above, with any modifiers. A VB6 menu's shortcut is one its Menu Editor lists, and is set only in the designer.|
|`CheckOnClick`|True ticks or unticks the item as it is chosen, before its Click. False, the default, leaves `Checked` to the program, as a VB6 menu does.|

> [!NOTE]
> **Not implemented.** The `PopupMenu` method, a control's context menu, and a picture on an item. The `WindowList` of an MDI form's menu, and the `NegotiatePosition` and `HelpContextID` of a menu, which the model keeps as written. Menu control arrays: a menu with an `Index` is read, and has none of an array's members.

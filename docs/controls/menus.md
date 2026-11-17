# Menus

A form's menus are its `RDForms.Menu` components. [`FormMenu.Of`](../api/RDCore.Forms.Menus.FormMenu.yml) gives them for a form's root, as the items of its menu bar, each holding the items of its own menu. Each item is a [`FormMenuItem`](../api/RDCore.Forms.Menus.FormMenuItem.yml): a menu component with VB's members and the rules VB6 has for them. The model has no Avalonia types.

A [`RDFormsFormWindow`](../api/RDCore.Forms.Avalonia.Rendering.RDFormsFormWindow.yml) shows them on its [`RDFormsMenuBar`](../api/RDCore.Forms.Avalonia.Rendering.RDFormsMenuBar.yml), above the client area. The client area keeps the root's `Width` and `Height` whether or not the bar is shown.

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

The order is VB6's (measured with VB6).

|Situation|Raised|
|---|---|
|A menu that holds items opens|Its Click, as it opens. Keeping it open, or closing it, raises nothing more.|
|An item that holds none is chosen|Its Click, alone: the menus above it raise none.|
|An item's shortcut is pressed|Each menu's Click from the bar down, then the item's.|

A shortcut stops at the first item on its way that is hidden or disabled. Each item's `Visible` and `Enabled` are read after the Click above it has run, so a menu's Click can enable the item below it in time.

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
|`NegotiatePosition`, read or set|393, Get not supported at runtime; 382, Set not supported at runtime. A designer file's value is read and kept.|

A program reads and sets an item's `HelpContextID`, any Long.

The last visible item of the bar can be hidden, and the bar goes with it.

## Menu arrays

Items that share a name and each have an `Index` are a menu array, a [`FormMenuArray`](../api/RDCore.Forms.Menus.FormMenuArray.yml): `FormMenu.ArrayOf` gives it by name, and `FormMenuItem.Array` for one of its elements. Its elements are in the same menu, or all on the bar. A program's `Load` and `Unload` of an element are its `Load` and `Unload`; the bar shows a loaded element at once, and drops an unloaded one. The arrays of other controls are [control arrays](index.md#control-arrays).

```csharp
var recent = FormMenu.Of(form.Root).ArrayOf("mnuRecent")!;
recent.Load(1).Caption = "&1 Report.txt";  // Load mnuRecent(1)
recent.Unload(1);                           // Unload mnuRecent(1)
```

- A loaded element goes among the others in the order of their indexes: after the one with the highest index below its own, or, with none below, before the one with the lowest index above.
- It copies the element with the lowest index loaded at the time: its `Caption`, `Checked`, `Enabled`, `Visible`, `Tag` and `HelpContextID`. It does not copy its `Shortcut`, or the items of its menu, so a copy of a menu opens none. RDForms' own: it also copies `CheckOnClick` and `Picture`.
- `Count` is the number of elements loaded, `LBound` the lowest index and `UBound` the highest. A program's `For Each` goes through them in index order.

|Statement|Error|
|---|---|
|`Load` of an element that is loaded|360, Object already loaded|
|`Load` with a negative index|341, Invalid object array index|
|`Load` with an index above 32767|6, Overflow|
|`Unload` of an element of the form's file|362, Can't unload controls created at design time|
|`Unload` of an element that is not loaded|340, Control array element doesn't exist|
|`Unload` of the last visible item of a menu|361, Can't load or unload this object|
|`Load` or `Unload` of an item that is not an element of an array|361, Can't load or unload this object|
|`Load` or `Unload` of an array's name with no index|361, Can't load or unload this object|

The last visible item of the bar can be unloaded, and the bar goes with it.

## Pop-up menus

A shown form's window is its root's [`IMenuHost`](../api/RDCore.Forms.Menus.IMenuHost.yml), registered with [`MenuHosts`](../api/RDCore.Forms.Menus.MenuHosts.yml) while it is open. Its `PopupMenu` is VB's `PopupMenu` method: it pops up the items of one of the form's menus, usually a hidden top-level one.

```csharp
var host = MenuHosts.Of(form.Root);
var edit = FormMenu.Of(form.Root).Items.Single(item => item.Component.Name == "mnuEdit");
await host!.PopupMenu(edit, x: 1200, y: 600, PopupMenuAlignment.Center);
```

|Argument|Meaning|
|---|---|
|`x`, `y`|The point, in the root's `ScaleMode`. A coordinate not given is the pointer's.|
|`alignment`|`Left`, the default, puts the menu's left edge at the point; `Center` centres it; `Right` puts its right edge there.|
|`boldItem`|An item of the menu, at any depth, shown in bold: VB's `BoldCommand`.|

The menu raises its Click as it opens, as it does on the bar, and an item chosen from it raises its own. The task completes when the menu closes, after that Click. A menu that holds no items shows nothing and raises nothing. A menu that is not one of the form's is refused with an `ArgumentException`.

## RD-VBA additions

|Member|Behaviour|
|---|---|
|`Shortcut`|Any key of the names above, with any modifiers. A VB6 menu's shortcut is one its Menu Editor lists, and is set only in the designer.|
|`CheckOnClick`|True ticks or unticks the item as it is chosen, before its Click. False, the default, leaves `Checked` to the program, as a VB6 menu does.|
|`Picture`|A picture shown beside the item's caption, as the bytes of its image file. One the decoder refuses is not drawn.|
|`ContextMenu`|On the form and on each control that raises mouse events: the name of one of the form's menus, compared without regard to case. A right release over the component pops it up at the pointer, after the component's MouseUp and Click. Empty, the default, or a name that is no menu of the form's pops up none.|

> [!NOTE]
> **Not implemented.** The `WindowList` of an MDI form's menu, which the model keeps as written.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

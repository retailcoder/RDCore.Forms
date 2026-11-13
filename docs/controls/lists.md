# Lists

ListBox and ComboBox. Their items are the component's `List`, with an `ItemData` value and an `ItemIcon` picture for each item. The behaviour below was measured with VB6, except for item icons and a combo's completion, which VB6 does not have.

## Items

Both controls have the same members for their items:

|Member|Behaviour|Error|
|---|---|---|
|`AddItem(item, index)`|Adds an item, at the end where no index is given. Its data is 0, and it has no icon.|5 for an index outside 0 to `ListCount`.|
|`RemoveItem(index)`|Removes an item, with its data and its icon.|5 for an index outside the items.|
|`Clear()`|Removes every item.||
|`GetItemData`, `SetItemData`|An item's data, the Long value attached to it.|381 for an index outside the items.|
|`GetItemIcon`, `SetItemIcon`|An item's icon, the bytes of a picture file.|381 for an index outside the items; 481 for a picture that is refused (see [Pictures](containers.md#pictures)).|
|`ListCount`|The number of items.||
|`ListIndex`|The selected item, or -1 for none. Setting it raises Click when it moves the selection.|380 for a value outside -1 to `ListCount` - 1.|

## Item icons

**RD-VBA addition.** An item's icon is drawn before its text, scaled to fit a square as tall as the text. Where any item has an icon, every item's text starts after that square, so that the text lines up. An icon is any picture a [PictureBox can draw](containers.md#pictures); a metafile icon is not drawn.

```csharp
var list = new VBListBox(new ComponentInstance(VBClasses.ListBox, "List1"));
list.AddItem("Open");
list.SetItemIcon(0, File.ReadAllBytes("open.png"));
```

## ListBox

[`VBListBox`](../api/RDCore.Forms.Avalonia.Controls.VBListBox.yml)

|Case|Behaviour|
|---|---|
|A left press on an item|Selects it at once; the selection follows the pointer over the items while the button is held. Click is raised at the release, before MouseUp, whether or not the selection changed and wherever the release lands.|
|A press below the items, a right or middle press|Selects nothing and raises no Click.|
|A key or code that moves the selection|Raises Click, with `ListIndex` already the new value. Setting it again, a key that can go no further, and a change to the items raise none. A key's Click comes before its KeyUp: see [A control's key events](form-events.md#a-controls-key-events).|
|A character typed|Where `MultiSelect` is None, selects the next item that starts with it, after the selected one and from the top again past the last, without regard to case.|
|`GetSelected`, `SetSelected`|Read and set an item's selection; 381 for an index outside the items. `SetSelected` raises Click when it changes something.|
|`SelCount`|The number of items selected.|
|`Text`|The text of the item at `ListIndex`, or "". `SetText` selects the first item whose whole text it is, without regard to case, and raises Click; on a multi-select list it raises Click and changes nothing.|

### MultiSelect

With a `MultiSelect` of Simple or Extended, `ListIndex` is the item with the focus, 0 from the start, and the selection is a set of items apart from it.

|MultiSelect|Mouse|Keys|
|---|---|---|
|Simple|A press toggles the item, whatever the modifiers.|The arrow keys move only the focus; Space toggles.|
|Extended|A press selects the item alone, Ctrl+press toggles it, and Shift+press selects from the anchor (the last item pressed or reached without Shift). A drag selects from the anchor.|Up and Down select the item alone; with Shift, from the anchor. Space changes nothing.|

Every key such a list takes raises Click, whether or not anything changed.

## ComboBox

[`VBComboBox`](../api/RDCore.Forms.Avalonia.Controls.VBComboBox.yml)

|Style|Behaviour|
|---|---|
|Dropdown Combo|The component's `Text` is the edit box's. Typing sets it; `Text` from code selects no item; choosing an item puts its text in the box. The edit box shows text only, with no icon.|
|Simple Combo|The edit box, as a Dropdown Combo's, with its list always shown under it and no button: see [Simple Combo](#simple-combo).|
|Dropdown List|No edit box. `Text` is the selected item's. `SetText` selects the first item whose whole text it is, without regard to case, and raises Click; it throws 383 where no item has that text. The box draws the selected item's icon. Characters typed select an item: see [Completion](#completion).|

|Event|Raised|
|---|---|
|Click|For every pick of an item by the mouse, the one already selected too; for every arrow key that moves the selection, the list open or closed; for a Dropdown List's character that selects another item; for Enter on a Dropdown Combo's highlighted item; and for `ListIndex` from code when it moves.|
|Change|From the edit box's text, by typing or by code, and never from choosing an item.|
|DropDown|As the list opens.|
|GotFocus, LostFocus|Once as the combo is entered and left, whether the combo or its edit box has the focus.|
|KeyDown, KeyPress, KeyUp|For the keys of the combo, its edit box and its open list. The form raises its own for them only where its `KeyPreview` is True. A key's Click, Change or DropDown comes before its KeyUp. Alt+Down raises KeyDown 18, KeyDown 40, DropDown, KeyUp 40, then KeyUp 18.|

### Simple Combo

The edit box is 21 pixels high, as VB6 draws it at its default font, and the list takes the rest of the combo's height. The list scrolls where its items do not fit.

|Input|Raised|
|---|---|
|A press on an item|Click, and the edit box takes the item's text, selected. The item already selected is picked again, with another Click. There is no Change.|
|A press below the items|The last item is picked, with Click.|
|A double click on an item|One Click, then DblClick.|
|A press on the list of a combo without the focus|GotFocus, then Click. The focus is left in the edit box.|
|Typing|Change. No item is selected.|
|Up and Down|Click, as in the other styles.|
|F4 and Alt+Down|Nothing: the list never drops, and there is no DropDown.|

> [!NOTE]
> **Not implemented.** VB6 snaps a Simple Combo's height to whole rows of its list, and a GotFocus raised by a press on the list reads the `Text` from before the pick. RDForms keeps the height the form gives it, and its GotFocus reads the picked item's `Text`.

### Locked

A `Locked` combo keeps out the user, not code. Its edit box takes no typing, its list does not drop, and neither the keys nor the pointer pick an item. It still raises its key events, and code's `Text` and `ListIndex` set it as they do any combo. These rules are RDForms' own, not measured with VB6.

### Completion

**RD-VBA addition.** Typing in a combo completes an item's text as RD-VBA's code completion completes a name. What is typed matches an item, without regard to case:

- by its start: `mu` matches `MultiPlayer`;
- or by its humps, each part of what is typed starting the next one, from the first hump: `MP` and `MuPl` match `MultiPlayer`, `Pl` does not. A hump starts at the first letter, at a capital after a lowercase letter or a digit, at the last capital of a run that a lowercase letter follows (`IOError` is `IO`, `Error`), at a run of digits (`Win32Api` is `Win`, `32`, `Api`), and after an underscore.

An item matched by its start is a better match than one matched by its humps; between two equal matches, the first in the list is better.

|Style|Typing|
|---|---|
|Dropdown Combo|Opens the list on the items the text matches, best first, and highlights the best. Up and Down move the highlight; Enter chooses the highlighted item, as a click does, and raises Click. Until then the text stays what was typed, and no item is selected. Where no item matches, the list closes.|
|Dropdown List|The characters typed add up into a word, which selects its best match and raises Click. A character that no item matches with the word starts a new word, which selects the next item it matches after the selected one, as a ListBox's character does. The word starts again after any other change of the selection.|
|Simple Combo|No completion: the text stays what was typed, and the list shows every item.|

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

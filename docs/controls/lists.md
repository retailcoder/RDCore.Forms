# Lists

ListBox and ComboBox. Their items are the component's `List`, with an `ItemData` value and an `ItemIcon` picture for each item. The behaviour below was measured with VB6, except for item icons, which VB6 does not have.

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
|A key or code that moves the selection|Raises Click, with `ListIndex` already the new value. Setting it again, a key that can go no further, and a change to the items raise none.|
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
|Dropdown List|No edit box. `Text` is the selected item's. `SetText` selects the first item whose whole text it is, without regard to case, and raises Click; it throws 383 where no item has that text. The box draws the selected item's icon.|

|Event|Raised|
|---|---|
|Click|For every pick of an item by the mouse, the one already selected too; for every arrow key that moves the selection, the list open or closed; for a Dropdown List's letter key; and for `ListIndex` from code when it moves.|
|Change|From the edit box's text, by typing or by code, and never from choosing an item.|
|DropDown|As the list opens.|
|GotFocus, LostFocus|Once as the combo is entered and left, whether the combo or its edit box has the focus.|

> [!NOTE]
> **Not implemented.** A `Style` of Simple Combo is shown as a Dropdown Combo, and `Locked` is not applied.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

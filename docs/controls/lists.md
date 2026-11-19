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
|`NewIndex`|The index of the item the last `AddItem` added, and -1 where none was added. After a `RemoveItem`, a `Clear` or a new `List` of the component, it is -1 again.||
|`GetList`, `SetList`|An item's text. `GetList` gives "" for an index outside the items. `SetList` keeps the item's data, its icon and its selection, and raises nothing. Where the item is the selected one, a combo's edit box takes its new text, as a choice of the item puts it there.|381 for an index outside the items, on `SetList`.|

`NewIndex` after a `RemoveItem`, a `Clear` or a new `List`, `GetList` and `SetList` follow RDForms' own rules.

## A program's calls

A program's code reaches a list's members by name through the form's window, as it does a [method](methods.md): `RDFormsFormWindow.Invoke` runs `AddItem`, `RemoveItem` and `Clear`, and reads a property; `RDFormsFormWindow.Assign` assigns one. Each is a [`MethodDefinition`](../api/RDCore.Forms.Model.MethodDefinition.yml) of the class, shared from [`Methods`](../api/RDCore.Forms.Model.Methods.yml). A property's definition has a `Value`: the type it holds.

```csharp
// cboPlayer.AddItem "Human", then cboPlayer.ListIndex = 0 and x = cboPlayer.List(0) in VB6 code.
var combo = form.Components.First(c => c.Name == "cboPlayer");
window.Invoke(combo, Methods.AddItem, [FormValue.FromString("Human")]);
window.Assign(combo, Methods.ListIndex, [], FormValue.FromInteger(0));
var x = window.Invoke(combo, Methods.List, [FormValue.FromInteger(0)]).AsString();
```

|Member|Kind|ListBox|ComboBox|
|---|---|---|---|
|`AddItem Item, [Index]`|Method|yes|yes|
|`RemoveItem Index`|Method|yes|yes|
|`Clear`|Method|yes|yes|
|`List(Index)` As String|Property|yes|yes|
|`ItemData(Index)` As Long|Property|yes|yes|
|`ItemIcon(Index)`, an object|Property, RD-VBA addition|yes|yes|
|`Selected(Index)` As Boolean|Property|yes|no|
|`ListIndex` As Integer|Property|yes|yes|
|`ListCount` As Integer|Read-only property|yes|yes|
|`NewIndex` As Integer|Read-only property|yes|yes|
|`SelCount` As Integer|Read-only property|yes|no|
|`Text` As String|Property|yes|yes|

|Rule|Behaviour|
|---|---|
|Index|An Integer, rounded half to even. Null is error 94, and a number past an Integer's range error 6.|
|Arguments|`List` without its index is error 449, and with two arguments error 450.|
|Read-only|An assignment is error 383, "'ListCount' property is read-only" for `ListCount`. A value of the wrong type is error 13 first.|
|`Text`|A ListBox's and a Dropdown List's is the selected item's text, and setting it selects the first item whose whole text it is (383 on a Dropdown List where none has it). Another combo's is its edit box's: see [ComboBox](#combobox).|
|`ItemIcon`|An object (`FormValue.FromObject`): the bytes of the item's picture file, or `Nothing` for none. Assigning anything else is error 13, and a picture that is refused error 481 (see [Item icons](#item-icons)).|
|`Column`|Error 438: VB6's ListBox has no `Column`, which is an MSForms ListBox's.|

The index and argument rules follow RDForms' own rules, as a method's do. A ListBox or ComboBox that `Controls.Add` adds takes the same members as one of the form's file.

> [!NOTE]
> **Not implemented.** `Sorted` and `TopIndex` are not modeled: a program's `TopIndex` is error 438, and a list whose file sets `Sorted` to True holds its items in the order they are added.

## Item icons

**RD-VBA addition.** An item's icon is drawn before its text, scaled to fit a square as tall as the text. Where any item has an icon, every item's text starts after that square, so that the text lines up. An icon is any picture a [PictureBox can draw](containers.md#pictures); a metafile icon is not drawn.

```csharp
var list = new RDFormsListBox(new ComponentInstance(ComponentClasses.ListBox, "List1"));
list.AddItem("Open");
list.SetItemIcon(0, File.ReadAllBytes("open.png"));
```

## ListBox

[`RDFormsListBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsListBox.yml)

|Case|Behaviour|
|---|---|
|A left press on an item|Selects it at once; the selection follows the pointer over the items while the button is held. Click is raised at the release, before MouseUp, whether or not the selection changed and wherever the release lands.|
|A press below the items, a right or middle press|Selects nothing and raises no Click.|
|Navigation keys|Up and Left move one item back, Down and Right one item on, Home and End to the first and the last item. PageUp and PageDown move as many items as the list shows whole, less one, so that PageDown from the top reaches the last item shown whole. The same for every `MultiSelect`.|
|A key or code that moves the selection|Raises Click, with `ListIndex` already the new value. Setting it again, a key that can go no further, and a change to the items raise none. A key's Click comes before its KeyUp: see [A control's key events](form-events.md#a-controls-key-events).|
|A character typed|Where `MultiSelect` is None, selects the next item that starts with it, after the selected one and from the top again past the last, without regard to case.|
|`GetSelected`, `SetSelected`|Read and set an item's selection; 381 for an index outside the items. `SetSelected` raises Click when it changes something.|
|`SelCount`|The number of items selected.|
|`Text`|The text of the item at `ListIndex`, or "". `SetText` selects the first item whose whole text it is, without regard to case, and raises Click; on a multi-select list it raises Click and changes nothing.|

### MultiSelect

With a `MultiSelect` of Simple or Extended, `ListIndex` is the item with the focus, 0 from the start, and the selection is a set of items apart from it.

On an RDForms form, a program can set `MultiSelect` (see [Properties set only at design time](index.md#properties-set-only-at-design-time)). The list then selects as one made with the new value, and keeps its items. The item at `ListIndex` stays selected where it was. To None, no other item stays selected. From None, the selected item takes the focus, or the first item where none is selected.

|MultiSelect|Mouse|Keys|
|---|---|---|
|Simple|A press toggles the item, whatever the modifiers.|The navigation keys move only the focus; Space toggles.|
|Extended|A press selects the item alone, Ctrl+press toggles it, and Shift+press selects from the anchor (the last item pressed or reached without Shift). A drag selects from the anchor.|A navigation key selects the item it reaches alone; with Shift, from the anchor (RDForms' own for the keys other than Up and Down). Space changes nothing.|

Every key such a list takes raises Click, whether or not anything changed: at the first or the last item too. The other keys change nothing and raise no Click (RDForms' own).

### Height

A ListBox's rows are a line of its font high, as a [Simple Combo's](#simple-combo) are, in a border of 2 pixels on each side. The height it is given, from the form or from code, is first rounded to whole pixels, 15 twips to a pixel.

- Where `IntegralHeight` is True, its default, the list is its border and as many whole rows as fit, and at least one: 1000 twips are 840, and 200 are 255.
- Where it is False, the list takes the height it is given.
- Where `IntegralHeight` is True, a font from code snaps the height to the new rows: 69 pixels at MS Sans Serif 8 are 64 at 12.
- `Height` keeps the height given where it rounds to the pixels the list takes, and reads those pixels otherwise: 1234 twips stay 1234, drawn as 82 pixels.

## ComboBox

[`RDFormsComboBox`](../api/RDCore.Forms.Avalonia.Controls.RDFormsComboBox.yml)

|Style|Behaviour|
|---|---|
|Dropdown Combo|The component's `Text` is the edit box's. Typing sets it; `Text` from code selects no item; choosing an item puts its text in the box. The edit box shows text only, with no icon.|
|Simple Combo|The edit box, as a Dropdown Combo's, with its list always shown under it and no button: see [Simple Combo](#simple-combo).|
|Dropdown List|No edit box. `Text` is the selected item's. `SetText` selects the first item whose whole text it is, without regard to case, and raises Click; it throws 383 where no item has that text. The box draws the selected item's icon. Characters typed select an item: see [Completion](#completion).|

On an RDForms form, a program can set `Style` (see [Properties set only at design time](index.md#properties-set-only-at-design-time)). The combo then works as one made with the new style, keeping its items and its selection, and its text where it has an edit box before and after. An edit box it gains takes the selected item's text, or "" with none. The change raises no event.

|Event|Raised|
|---|---|
|Click|For every pick of an item by the mouse, the one already selected too; for every arrow key that moves the selection, the list open or closed; for a Dropdown List's character that selects another item; for Enter on a Dropdown Combo's highlighted item; and for `ListIndex` from code when it moves.|
|Change|From the edit box's text, by typing or by code, and never from choosing an item.|
|DropDown|As the list opens.|
|GotFocus, LostFocus|Once as the combo is entered and left, whether the combo or its edit box has the focus.|
|KeyDown, KeyPress, KeyUp|For the keys of the combo, its edit box and its open list. The form raises its own for them only where its `KeyPreview` is True. A key's Click, Change or DropDown comes before its KeyUp. Alt+Down raises KeyDown 18, KeyDown 40, DropDown, KeyUp 40, then KeyUp 18.|

### Selection

`SelStart`, `SelLength` and `SelText` are the edit box's selection, with a single-line TextBox's rules: see [Selection](text.md#selection). A Dropdown List has no edit box, and reading or setting any of them is 380.

|Case|Behaviour|
|---|---|
|`ListIndex` from code|The edit box takes the item's text with nothing selected, even where the item's text is the text already in the box, the caret before the first character, then Click is raised: its handler reads a `SelStart` and a `SelLength` of 0.|
|`SelText` set over a chosen item|The combo selects no item, `ListIndex` -1, and raises its Change, even where the text does not change.|
|`Text` from code, the same text|The selection and `ListIndex` stay as they are.|

### Simple Combo

The list is under the edit box, in a border of 2 pixels on each side, and scrolls where its items do not fit. Its rows are a line of the combo's font high, and the edit box is a row and 8 pixels high: 13 and 21 pixels at the default font, MS Sans Serif 8, and 20 and 28 at MS Sans Serif 12.

The height the combo is given is first rounded to whole pixels, 15 twips to a pixel. Then:

|Case|`IntegralHeight` True (the default)|`IntegralHeight` False|
|---|---|---|
|A height more than the edit box's|The edit box, the list's border and as many whole rows as fit: 1000 twips are 960. Where no row fits, the edit box alone.|The height given. Where the list would be no more than its border, the edit box alone.|
|A height of the edit box's or less, from the form|The edit box alone.|The edit box alone.|
|A height of the edit box's or less, from code|Ignored: the combo keeps its size.|Ignored.|
|A font from code|The edit box and the rows take the font, the list keeps its pixels, and the height snaps to the new rows.|The edit box takes the font, and the list keeps its pixels.|

`Height` keeps the height given where it rounds to the pixels the combo takes, and reads those pixels otherwise: 1150 twips stay 1150, drawn as 77 pixels.

MS Sans Serif's rows are VB6's at each size it is drawn at, its line height in [Fonts](fonts.md). Another font's rows are RDForms' own: the line as RDForms draws it, rounded up to a whole pixel. VB6 takes them from the font's hinting, so they can differ from VB6's by a pixel or more.

|Input|Raised|
|---|---|
|A press on an item|Click, and the edit box takes the item's text, selected. The item already selected is picked again, with another Click. There is no Change.|
|A press below the items|The last item is picked, with Click.|
|A double click on an item|One Click, then DblClick.|
|A press on the list of a combo without the focus|The item is selected first: the LostFocus of the control that loses the focus, then the combo's GotFocus, read the combo's new `ListIndex` and its `Text` from before the pick. Then Click, with the item's text. The focus is left in the edit box.|
|Typing|Change. No item is selected.|
|Up and Down|Click, as in the other styles.|
|F4 and Alt+Down|Nothing: the list never drops, and there is no DropDown.|

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

## Converting from MSForms

The names an MSForms ListBox and ComboBox take in RDForms (see [Converting from MSForms](index.md#converting-from-msforms)):

|MSForms|RDForms|
|---|---|
|`TextAlign`|`Alignment`, with VB's values: `fmTextAlignLeft` (1) is 0, `fmTextAlignCenter` (2) is 2, and `fmTextAlignRight` (3) is 1.|
|`ListWidth`, in points|`ListWidth`, in pixels.|
|`BorderStyle`, `fmBorderStyleNone` and `fmBorderStyleSingle`|`BorderStyle`, 0 and 1.|
|A ComboBox's `Value`|`Text`.|
|A ListBox's `Value`|None: an RDForms ListBox has no member for it.|
|`List`, `ListIndex`, `ListCount`, `Text` and the other members of the same name|The same.|

> [!NOTE]
> **Not implemented.** A ListBox and a ComboBox have no `IMEMode`, and a ComboBox no `Copy`, `Cut`, `Paste` or `CanPaste`. Their BeforeDragOver, BeforeDropOrPaste and Error are declared and never raised.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

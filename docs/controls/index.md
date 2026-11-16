# Controls

`RDCore.Forms.Avalonia` shows a form's components with Avalonia controls. Each control shows one [`ComponentInstance`](../api/RDCore.Forms.Model.ComponentInstance.yml): the component holds the values, and the control follows them. A change from code, made on the component, shows in the control; a change the user makes, such as typing in a TextBox, is written to the component.

## Catalogue

[`VBControls.Create`](../api/RDCore.Forms.Avalonia.Controls.VBControls.yml) makes the control for a component of one of Visual Basic 6.0's intrinsic classes.

|Class|Control|Events raised|Page|
|---|---|---|---|
|`VB.CommandButton`|[`VBCommandButton`](../api/RDCore.Forms.Avalonia.Controls.VBCommandButton.yml)|Click, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`VB.CheckBox`|[`VBCheckBox`](../api/RDCore.Forms.Avalonia.Controls.VBCheckBox.yml)|Click, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`VB.OptionButton`|[`VBOptionButton`](../api/RDCore.Forms.Avalonia.Controls.VBOptionButton.yml)|Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`VB.Label`|[`VBLabel`](../api/RDCore.Forms.Avalonia.Controls.VBLabel.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Text](text.md)|
|`VB.TextBox`|[`VBTextBox`](../api/RDCore.Forms.Avalonia.Controls.VBTextBox.yml)|Change, Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Text](text.md)|
|`VB.ListBox`|[`VBListBox`](../api/RDCore.Forms.Avalonia.Controls.VBListBox.yml)|Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Lists](lists.md)|
|`VB.ComboBox`|[`VBComboBox`](../api/RDCore.Forms.Avalonia.Controls.VBComboBox.yml)|Change, Click, DblClick, DropDown, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus|[Lists](lists.md)|
|`VB.DriveListBox`|[`VBDriveListBox`](../api/RDCore.Forms.Avalonia.Controls.VBDriveListBox.yml)|Change, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus|[File system lists](file-lists.md)|
|`VB.DirListBox`|[`VBDirListBox`](../api/RDCore.Forms.Avalonia.Controls.VBDirListBox.yml)|Change, Click, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[File system lists](file-lists.md)|
|`VB.FileListBox`|[`VBFileListBox`](../api/RDCore.Forms.Avalonia.Controls.VBFileListBox.yml)|Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp, PathChange, PatternChange|[File system lists](file-lists.md)|
|`VB.Frame`|[`VBFrame`](../api/RDCore.Forms.Avalonia.Controls.VBFrame.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Containers and pictures](containers.md)|
|`VB.PictureBox`|[`VBPictureBox`](../api/RDCore.Forms.Avalonia.Controls.VBPictureBox.yml)|Click, DblClick, GotFocus, LostFocus, KeyDown, KeyPress, KeyUp, MouseDown, MouseMove, MouseUp, Paint, Resize|[Containers and pictures](containers.md)|
|`VB.Image`|[`VBImage`](../api/RDCore.Forms.Avalonia.Controls.VBImage.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Containers and pictures](containers.md)|
|`VB.Timer`|[`VBTimer`](../api/RDCore.Forms.Avalonia.Controls.VBTimer.yml)|Timer|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`VB.HScrollBar`, `VB.VScrollBar`|[`VBScrollBar`](../api/RDCore.Forms.Avalonia.Controls.VBScrollBar.yml)|Change, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, Scroll|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`VB.Shape`|[`VBShape`](../api/RDCore.Forms.Avalonia.Controls.VBShape.yml)|None|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`VB.Line`|[`VBLine`](../api/RDCore.Forms.Avalonia.Controls.VBLine.yml)|None|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`VB.Data`|[`VBData`](../api/RDCore.Forms.Avalonia.Controls.VBData.yml)|None|[Data control](data.md)|
|`VB.Menu`|The window's [`VBMenuBar`](../api/RDCore.Forms.Avalonia.Rendering.VBMenuBar.yml)|Click|[Menus](menus.md)|
|A placed UserControl, such as `Project1.Gauge`|[`VBUserControl`](../api/RDCore.Forms.Avalonia.Controls.VBUserControl.yml)|None of its own; its UserControl's root and controls raise theirs|[UserControls](usercontrols.md)|

A `VB.Menu` has no control of its own, and `Create` returns `null` for it: a form's window shows its menus on its [menu bar](menus.md). It returns `null` for a UserControl that is invisible at run time too.

## Showing a form

A [`VBFormWindow`](../api/RDCore.Forms.Avalonia.Rendering.VBFormWindow.yml) shows a form whose root is a `VB.Form`. Its client area is a [`VBFormView`](../api/RDCore.Forms.Avalonia.Rendering.VBFormView.yml), which holds the form's controls. The controls raise their events to the [`IComponentEventSink`](../api/RDCore.Forms.Model.IComponentEventSink.yml) set on the window; a window with no sink raises none.

```csharp
using RDCore.Forms.Avalonia.Controls;
using RDCore.Forms.Avalonia.Rendering;
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;

// In a running Avalonia application.
var form = VB6FormReader.ReadFile("Form1.frm").Form!;
var window = new VBFormWindow(form);
VBControl.SetEventSink(window, new PrintingSink());
window.Show();

sealed class PrintingSink : IComponentEventSink
{
    public void Raise(ComponentInstance source, EventDefinition definition, FormValue[] arguments) =>
        Console.WriteLine($"{source.Name}_{definition.Name}");
}
```

The sink is called with the component, the event, and its arguments in the order of the event's parameters. A handler's assignment to an argument, such as a `KeyAscii` set to 0, is written back into the array, and the control reads it there.

The window and the controls need an Avalonia application with a theme. A host that has none uses [`RDFormsApplication`](../api/RDCore.Forms.Avalonia.RDFormsApplication.yml): Avalonia's Simple theme, drawn with Skia, with Inter as its default font. Its `Configure` method returns the application's builder, and the host adds the platform:

```csharp
using Avalonia;
using RDCore.Forms.Avalonia;

// Avalonia's desktop platform, from the Avalonia.Desktop package.
RDFormsApplication.Configure().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
```

A host that has an Avalonia application of its own keeps it.

The window follows the root's `Caption`, `Icon`, `Width`, `Height`, `MaxButton`, `MinButton` and `ShowInTaskbar`, and a user who resizes it sets the root's `Width` and `Height`. An `Icon` is an icon or a cursor; setting it to any other picture is 380. A window with no `Icon` shows the window system's own. The client area shows the root's `Picture` at its size from its top left corner, under the drawing and the controls (see [Pictures](containers.md#pictures)). A `StartUpPosition` of CenterOwner or CenterScreen centres it; Manual and Windows Default leave its place to the window system. The root raises its own events, Resize, Activate, QueryUnload, Unload and its mouse and key events among them: see [Form events](form-events.md).

## Placement

|Rule|Behaviour|
|---|---|
|Units|The model holds a control's `Left`, `Top`, `Width` and `Height` in pixels (1/96 inch, 15 twips), whatever its container's `ScaleMode`. A program's code reads them in its own unit: VB6 code in the container's scale (twips in a Frame, and for a form's own), VBA code in points, and code made for RDForms in pixels ([`CodeUnits`](../api/RDCore.Forms.Model.CodeUnits.yml)). A Line's ends are in its container's scale.|
|Containers|A control placed in a Frame or a PictureBox is shown in it. A control placed in any other component is not shown.|
|Stacking|In each container, the first control the form holds is on top until [`ZOrder`](methods.md#zorder) moves one. A windowless control (a Label, an Image, a Shape or a Line) is under every windowed one.|
|Tab order|One order across the whole form, by `TabIndex`, the form's order deciding a tie. A `TabIndex` past the last place takes the first free place, in the form's order.|
|Focus|Tab skips a control that takes no focus (a Label, a Frame, an Image, a Timer, a Shape, a Line or a Data control), one whose `TabStop` is False, and one that is hidden or disabled or in a hidden or disabled container.|

## A control's container at run time

A program's `Set Control.Container = Frame1` is `FormDefinition.SetContainer(control, container)`. It places the control, with the controls it holds, in the form or in a Frame or a PictureBox of the same form, and raises `FormDefinition.ContainerChanged`. A shown form's view moves the control at once. Every control has a `Container` except a Timer (`ComponentClass.HasContainer`); a menu item has none.

|Rule|Behaviour|
|---|---|
|Place|The control keeps its place in pixels in its container's client area. A program reads its `Left` and `Top` in the new container's scale: 200, 300 twips in a form is 13.33333, 20 in a PictureBox whose `ScaleMode` is pixels. A Line's ends are given again in the new container's scale.|
|Stacking|On top of the windowed controls of its new container, even where that is the container it was in.|
|Unchanged|`Visible`, every `TabIndex`, and the removal of a control added at run time.|
|Focus|A control that has the focus and is placed in a Frame or a PictureBox keeps it, and no LostFocus or GotFocus is raised.|
|Order|The control is last in its new container's contents, so `FormDefinition.Controls` and a tie of `TabIndex` follow the new order (RDForms' own).|

`SetContainer` refuses with error 425, Invalid object use, and changes nothing, where the container is `Nothing`, holds no controls (a TextBox, a menu item), is the control itself or a control it holds, or is another form or a control of another form.

> [!NOTE]
> **Not implemented.** A control that has the focus keeps it when it is placed back in the form, too. In VB6, a TextBox placed from a Frame back in the form, outside the Frame's area, raised LostFocus and gave the focus to the Frame (observed once).

## Common properties

|Property|Behaviour|
|---|---|
|`Visible`, `Enabled`|Hide or disable the control, and every control it holds. A disabled form takes no input in any of its controls.|
|`Opacity`|RD-VBA addition. How solid the control is drawn: a Single from 0, not seen, to 1, solid, which is the default. See [Opacity](#opacity).|
|`BackColor`, `ForeColor`|A system colour is drawn in Windows 11's default palette ([`FormPalette`](../api/RDCore.Forms.Avalonia.Controls.FormPalette.yml)), on every platform.|
|`Font`|The control's font, held at the size VB6 draws it at: see [Fonts](fonts.md).|
|`ToolTipText`|Shown as the control's tooltip; an empty text shows none.|
|`MousePointer`|The pointer over the control. Custom, drawn from `MouseIcon` in VB6, shows the arrow.|

### Opacity

**RD-VBA addition.** VB6's controls have no `Opacity`. Every control a running program shows has it: each intrinsic control, a Line and a placed UserControl that is visible at run time. A form, a menu item and a Timer have none.

|Case|Effect|
|---|---|
|0 to 1|The whole control is drawn that solid over what is under it: its background, border, caption and picture, and every control it holds. 0 is not seen, 1 is solid.|
|Outside 0 to 1|Error 380, Invalid property value, and the value is unchanged. A designer refuses it too. A form's file that holds one keeps the line as written.|
|Mouse|A control at 0 still takes the mouse and raises its mouse events. Only `Visible` False stops it taking the mouse.|
|Order|Unchanged: a windowless control at any `Opacity` stays under every windowed control.|

## Run-time controls

[`FormDefinition.AddControl`](../api/RDCore.Forms.Model.FormDefinition.yml) and `RemoveControl` add and remove controls as a program's `Controls.Add` and `Controls.Remove` do. A shown form's view places and removes the controls at once.

|Rule|Behaviour|
|---|---|
|Class|Named by its ProgID, such as `VB.CommandButton`, without regard to case. A menu's or a form's class is refused.|
|Name|Any name that no other control has, compared without regard to case, even one that starts with a digit, holds a space, is a keyword, or is the form's own name. An empty name is refused, and so is a name of white space only (RDForms' own).|
|Container|The form, or a Frame or a PictureBox of the form.|
|Defaults|Hidden, at 0, 0, and 80 by 32 pixels (1200 by 480 twips) where the class has an extent. The next `TabIndex` after the form's others. A caption or a text is "".|
|Stacking|Under the controls already in its container, windowed or windowless.|
|Order|`FormDefinition.Controls` holds the controls of the form's file depth first, then those added, in the order added, whatever their container.|
|Events|An added control raises its events to the window's sink as the form's other controls do. Adding one raises no event of the form.|
|Removal|Only a control added at run time, with the controls it holds, and not an element of a control array. The other `TabIndex` values close up. A control that had the focus gives it to the first control of the tab order that takes it (RDForms' own).|

|Error|When|
|---|---|
|711|No VB class has the ProgID.|
|720|The class is a menu's or a form's.|
|721|The container holds no controls, such as a CommandButton.|
|727|The name is taken.|
|728|The name is empty, or white space only.|
|729|The control to remove was not added at run time, or is an element of a [control array](#control-arrays), even one a program's `Load` added.|
|730|No control has the name to remove, or it was removed already.|
|740|The control to remove holds a control that was not added at run time. Nothing is removed.|
|-2147467262 (&H80004002)|The name to remove is a control array's: *No such interface supported*.|

> [!NOTE]
> **Not implemented.** A ListBox added at run time to a form that is not shown keeps a height of 32 pixels until the form is shown, where its height is snapped to whole rows (450 twips, 30 pixels); VB6 snaps it as it is added. The `Controls` collection itself, error 735 for a control used after its removal, and the controls of a form's file coming back after Unload and Load belong to the runtime that runs the program.

## Control arrays

Controls that share a name and each have an `Index` are a control array, a [`FormControlArray`](../api/RDCore.Forms.Model.FormControlArray.yml): `FormDefinition.ArrayOf` gives it by name, compared without regard to case, and the same object each time. Menu items are in [menu arrays](menus.md#menu-arrays). A program's `Load` and `Unload` of an element are its `Load` and `Unload`.

```csharp
var buttons = form.ArrayOf("cmdAction")!;
var copy = buttons.Load(3);  // Load cmdAction(3)
buttons.Unload(3);           // Unload cmdAction(3)
```

- `Elements` are in the order of their indexes, whatever the order of the form's file. `Count` is the number of elements loaded, `LBound` the lowest index and `UBound` the highest. A program's `For Each` goes through them in index order.
- `Load` copies the element with the lowest index loaded at the time, with every property set on it but two: the copy is hidden, and its `TabIndex` is the next after the form's others. It goes in the same container, under the controls already in it, and holds none of the controls the element holds. `FormDefinition.Controls` has it after the controls of the form's file, in the order loaded.
- `Unload` takes out an element a program loaded, which `Controls.Remove` does not (729). The other `TabIndex` values close up.

|Statement|Error|
|---|---|
|`Load` of an element that is loaded|360, Object already loaded|
|`Load` with a negative index|341, Invalid control array index|
|`Load` with an index above 32767|6, Overflow|
|`Load` when no element is left to copy|340, Control array element doesn't exist|
|`Unload` of an element of the form's file|362, Can't unload controls created at design time|
|`Unload` of an element that is not loaded|340, Control array element doesn't exist|

A refused `Load` or `Unload` changes nothing.

## Properties set only at design time

Some properties are set only by a designer and a form's file. A running VB6 program can read them and cannot set them: its assignment is error 382. A few it can neither read (error 393) nor set (382). These are the ones RDForms models, as measured in VB6:

|Access from code|Properties|
|---|---|
|Read only|Form and MDIForm: `StartUpPosition`. Form: `ShowInTaskbar`, `MaxButton`, `MinButton`. CommandButton, CheckBox, OptionButton and ComboBox: `Style`. TextBox: `MultiLine`, `ScrollBars`. ListBox and FileListBox: `MultiSelect`. ListBox and ComboBox: `IntegralHeight`. Menu: `WindowList`.|
|Neither read nor set|Menu: `Shortcut`. Timer: `Left`, `Top`. Data: `Negotiate`.|

The model answers for each class with [`ComponentClass.IsReadOnlyAtRunTime`](../api/RDCore.Forms.Model.ComponentClass.yml) and `IsNotAvailableAtRunTime`. A Timer's `Left` and `Top` are the same definitions as other controls', and only the Timer makes them unavailable.

> [!NOTE]
> **Not implemented.** The runtime does not refuse a program's read or assignment of these properties with 393 or 382.

## Errors

A member that refuses a value throws a [`FormErrorException`](../api/RDCore.Forms.Model.FormErrorException.yml) that holds VB's error number and description, such as 380 for a `ListIndex` outside the items.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | CONTROLS | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

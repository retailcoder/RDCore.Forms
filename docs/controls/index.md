# Controls

`RDCore.Forms.Avalonia` shows a form's components with Avalonia controls. Each control shows one [`ComponentInstance`](../api/RDCore.Forms.Model.ComponentInstance.yml): the component holds the values, and the control follows them. A change from code, made on the component, shows in the control; a change the user makes, such as typing in a TextBox, is written to the component.

## Catalogue

[`VBControls.Create`](../api/RDCore.Forms.Avalonia.Controls.VBControls.yml) makes the control for a component of one of Visual Basic 6.0's intrinsic classes.

|Class|Control|Events raised|Page|
|---|---|---|---|
|`VB.CommandButton`|[`VBCommandButton`](../api/RDCore.Forms.Avalonia.Controls.VBCommandButton.yml)|Click, GotFocus, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`VB.CheckBox`|[`VBCheckBox`](../api/RDCore.Forms.Avalonia.Controls.VBCheckBox.yml)|Click, GotFocus, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`VB.OptionButton`|[`VBOptionButton`](../api/RDCore.Forms.Avalonia.Controls.VBOptionButton.yml)|Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove, MouseUp|[Buttons](buttons.md)|
|`VB.Label`|[`VBLabel`](../api/RDCore.Forms.Avalonia.Controls.VBLabel.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Text](text.md)|
|`VB.TextBox`|[`VBTextBox`](../api/RDCore.Forms.Avalonia.Controls.VBTextBox.yml)|Change, Click, DblClick, GotFocus, KeyDown, KeyPress, KeyUp, LostFocus, MouseDown, MouseMove, MouseUp|[Text](text.md)|
|`VB.ListBox`|[`VBListBox`](../api/RDCore.Forms.Avalonia.Controls.VBListBox.yml)|Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove, MouseUp|[Lists](lists.md)|
|`VB.ComboBox`|[`VBComboBox`](../api/RDCore.Forms.Avalonia.Controls.VBComboBox.yml)|Change, Click, DropDown, GotFocus, LostFocus|[Lists](lists.md)|
|`VB.Frame`|[`VBFrame`](../api/RDCore.Forms.Avalonia.Controls.VBFrame.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Containers and pictures](containers.md)|
|`VB.PictureBox`|[`VBPictureBox`](../api/RDCore.Forms.Avalonia.Controls.VBPictureBox.yml)|Click, DblClick, GotFocus, LostFocus, MouseDown, MouseMove, MouseUp|[Containers and pictures](containers.md)|
|`VB.Image`|[`VBImage`](../api/RDCore.Forms.Avalonia.Controls.VBImage.yml)|Click, DblClick, MouseDown, MouseMove, MouseUp|[Containers and pictures](containers.md)|
|`VB.Timer`|[`VBTimer`](../api/RDCore.Forms.Avalonia.Controls.VBTimer.yml)|Timer|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`VB.HScrollBar`, `VB.VScrollBar`|[`VBScrollBar`](../api/RDCore.Forms.Avalonia.Controls.VBScrollBar.yml)|Change, GotFocus, LostFocus|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`VB.Shape`|[`VBShape`](../api/RDCore.Forms.Avalonia.Controls.VBShape.yml)|None|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|
|`VB.Line`|[`VBLine`](../api/RDCore.Forms.Avalonia.Controls.VBLine.yml)|None|[Timers, scroll bars and shapes](timers-scrollbars-shapes.md)|

> [!NOTE]
> **Not implemented.** `VB.Menu` has no control: `Create` returns `null` for it, and a form's window shows no menu. The form itself raises no events.

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

The window follows the root's `Caption`, `Width`, `Height`, `MaxButton`, `MinButton` and `ShowInTaskbar`. A `StartUpPosition` of CenterOwner or CenterScreen centres it; Manual and Windows Default leave its place to the window system.

## Placement

|Rule|Behaviour|
|---|---|
|Units|A control's `Left`, `Top`, `Width` and `Height` are twips, whatever its container's `ScaleMode`: 15 twips a pixel. A Line's ends are in its container's scale.|
|Containers|A control placed in a Frame or a PictureBox is shown in it. A control placed in any other component is not shown.|
|Stacking|In each container, the first control the form holds is on top. A windowless control (a Label, an Image, a Shape or a Line) is under every windowed one.|
|Tab order|One order across the whole form, by `TabIndex`, the form's order deciding a tie. A `TabIndex` past the last place takes the first free place, in the form's order.|
|Focus|Tab skips a control that takes no focus (a Label, a Frame, an Image, a Timer, a Shape or a Line), one whose `TabStop` is False, and one that is hidden or disabled or in a hidden or disabled container.|

## Common properties

|Property|Behaviour|
|---|---|
|`Visible`, `Enabled`|Hide or disable the control, and every control it holds. A disabled form takes no input in any of its controls.|
|`BackColor`, `ForeColor`|A system colour is drawn in Windows 11's default palette ([`FormPalette`](../api/RDCore.Forms.Avalonia.Controls.FormPalette.yml)), on every platform.|
|`Font`|The control's font.|
|`ToolTipText`|Shown as the control's tooltip; an empty text shows none.|
|`MousePointer`|The pointer over the control. Custom, drawn from `MouseIcon` in VB6, shows the arrow.|

## Errors

A member that refuses a value throws a [`FormErrorException`](../api/RDCore.Forms.Model.FormErrorException.yml) that holds VB's error number and description, such as 380 for a `ListIndex` outside the items.

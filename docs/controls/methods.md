# Methods

A program's code calls a method of the root or of a control through the form's window: [`VBFormWindow.Invoke`](../api/RDCore.Forms.Avalonia.Rendering.VBFormWindow.yml) runs it on the UI thread. Each class lists its methods in [`ComponentClass.Methods`](../api/RDCore.Forms.Model.ComponentClass.yml), shared from [`VBMethods`](../api/RDCore.Forms.Model.VB.VBMethods.yml).

```csharp
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

// Text1.Move 600, 900 in VB6 code, then Text1.SetFocus.
var text = form.Components.First(c => c.Name == "Text1");
window.Invoke(text, VBMethods.Move, [FormValue.FromSingle(600), FormValue.FromSingle(900)], CodeUnit.VB6);
window.Invoke(text, VBMethods.SetFocus, []);
```

An argument left out is `null`, and the list ends at the last one given. A `Variant` parameter takes any value, and the method coerces it.

|Class|Methods|
|---|---|
|Form, MDIForm|`Move`, `Refresh`, `SetFocus`|
|UserControl, PropertyPage|`Refresh`|
|A control that can take the focus|`Move`, `Refresh`, `SetFocus`, `ZOrder`|
|Label, Frame, Image, Shape|`Move`, `Refresh`, `ZOrder`|
|Line|`Refresh`, `ZOrder`|
|Timer, Menu|None|

## Move

`Move Left, [Top], [Width], [Height]` sets `Left`, then `Top`, `Width` and `Height`, as many as it is given (measured with VB6).

|Rule|Behaviour|
|---|---|
|Unit|The call's unit ([Units](index.md#placement)). In VB6's, a control's values are in its container's scale, and a form's are twips whatever its `ScaleMode`.|
|Rounding|In VB6's unit, each value is a whole number of twips, rounded half to even.|
|Checks|Every value is checked before any is set. A negative width or height is error 5, a value past a Long in twips is error 6, and Null is error 94.|
|Arguments|`Left` is required (449). An argument left out before one given, as in `Move 600, , 4800`, is error 448.|
|Resize|On the root, a `Move` that changes the size raises one Resize, where setting `Width` and then `Height` raises two. A `Move` to the size the form already has raises none.|

The window system's smallest size for a window is not modelled: a form takes the size asked for.

## SetFocus

|Situation|Behaviour|
|---|---|
|The form is not shown|Error 5, on a control and on the form.|
|The control is hidden or disabled|Error 5, and nothing changes.|
|Otherwise|[`ActiveControl`](../api/RDCore.Forms.Avalonia.Rendering.VBFormWindow.yml) names the control at once. After the call returns, the control that had the focus raises LostFocus, then this one GotFocus.|
|The control is on a form that is not the active one|That form is activated, and the control takes the focus after its Activate.|
|The control has the focus already|Nothing.|
|The form's own `SetFocus`, on the active form|Nothing.|
|An OptionButton|It is selected, and raises Click after its GotFocus.|

`SetFocus` raises no Validate. These rules are measured with VB6. Two are RDForms' own: a control in a hidden or disabled container refuses the focus with error 5, and the form's own `SetFocus` activates a shown form that is not the active one.

## ZOrder

`ZOrder [Position]` moves a control within its plane in its container (measured with VB6). A windowless control stays under every windowed one, and a container takes its controls with it.

|`Position`|Behaviour|
|---|---|
|0, or none|To the front.|
|1|To the back.|
|An Integer, rounded half to even|0.6 and `"1"` are 1; -0.5 and Empty are 0.|
|Another number, such as 2, -1 or 1.5|Error 5.|
|A string that is not a number, or Null|Error 13.|
|Past an Integer, such as 32768|Error 6.|

## Refresh

`Refresh` draws the component and its contents again. On a form or a PictureBox whose `AutoRedraw` is False, it raises Paint inside the call. VB6's `Refresh` was not measured: this rule is RDForms' own.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | CONTROLS | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

# UserControls

A UserControl's designer file (`.ctl`) reads as a [`FormDefinition`](../api/RDCore.Forms.Model.FormDefinition.yml) whose root is an `RDForms.UserControl`. A form places the UserControl as a control of the class [`UserControlClasses.Of`](../api/RDCore.Forms.Model.UserControlClasses.yml) makes from that definition, and a [`RDFormsUserControl`](../api/RDCore.Forms.Avalonia.Controls.RDFormsUserControl.yml) shows it.

## The root

The root has the properties VB6's Properties window lists for a UserControl, with VB6's defaults. A placed control's view follows `BackColor`, `BackStyle`, `Enabled`, `Picture` and `Font` (see [Showing a placed UserControl](#showing-a-placed-usercontrol)). The model holds the others and RDForms acts on none of them.

|Property|Default|
|---|---|
|`BackStyle`|1, Opaque.|
|`MaskColor`|The button face's system colour, `&H8000000F&`.|
|`PaletteMode`|3, Container.|
|`CanGetFocus`, `ClipControls`, `FontTransparent`, `HasDC`, `Enabled`|True.|
|`ClipBehavior`, `HitBehavior`|1, Use Region.|
|`Alignable`, `ControlContainer`, `DefaultCancel`, `EditAtDesignTime`, `ForwardFocus`, `InvisibleAtRuntime`, `Public`, `Windowless`|False.|
|`DataBindingBehavior`, `DataSourceBehavior`, `OLEDropMode`|0, None.|
|`AccessKeys`|"".|
|`MaskPicture`, `Palette`, `ToolboxBitmap`|No picture.|

Its drawing properties (`AutoRedraw`, `DrawMode`, `DrawStyle`, `DrawWidth`, `FillColor`, `FillStyle`), `Appearance`, `BorderStyle`, `Picture`, `MouseIcon`, `MousePointer`, `RightToLeft`, `KeyPreview`, `Font`, `BackColor` and `ForeColor` have a form's defaults.

> [!NOTE]
> **Not implemented.** `PropertyPages` is kept as text.

## Placed UserControls

[`UserControlClasses.Of`](../api/RDCore.Forms.Model.UserControlClasses.yml) makes the class from the UserControl's definition, the project's name and the events the UserControl's code declares. Its type name is the project's name and the UserControl's, such as `Project1.Gauge`, and [`VB6ImportOptions.Classes`](../api/RDCore.Forms.Import.VB6.VB6ImportOptions.yml) gives it to the reader of the forms that place it:

```csharp
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;

var gauge = UserControlClasses.Of(VB6FormReader.ReadFile("Gauge.ctl").Form!, "Project1");
var form = VB6FormReader.ReadFile("Form1.frm", new VB6ImportOptions { Classes = [gauge] }).Form!;
```

A placed control's members are those of the extender VB6 wraps it in. The UserControl's settings decide which it has, as measured with VB6:

|Setting|Members of a placed control|
|---|---|
|Any|`Left` and `Top`.|
|`InvisibleAtRuntime` False|`Width`, `Height`, `Visible`, `DragMode`, `ToolTipText`, `WhatsThisHelpID` and `Tag`, and RD-VBA's [`Opacity`](index.md#opacity).|
|`InvisibleAtRuntime` True|Nothing else. The control has no size, and its `Left` and `Top` are not available at run time (393).|
|`CanGetFocus` or `ForwardFocus` True, and visible|`TabIndex`.|
|`CanGetFocus` True, `ForwardFocus` False, and visible|`TabStop`, `CausesValidation`, `HelpContextID`, `SetFocus`, and the GotFocus and LostFocus events.|
|`DefaultCancel` True|`Default` and `Cancel`.|
|`Alignable` True|`Align`, and `Negotiate`: False, and not available at run time (393).|

A placed control has no `Enabled`, `hWnd` or data-binding member (438 in VB6). It is a container when the UserControl's `ControlContainer` is True, and windowless when its `Windowless` is. The properties a form's file sets on it that are the UserControl's own, such as its `_ExtentX` and `_ExtentY`, are kept as text.

> [!NOTE]
> **Not implemented.** A placed control has no `Move`, `ZOrder`, `Parent`, `Container` or `Object`.

## Showing a placed UserControl

[`RDFormsControls.Create`](../api/RDCore.Forms.Avalonia.Controls.RDFormsControls.yml) makes a `RDFormsUserControl` for a placed UserControl, and a form's window shows it at the control's place and size. Each placed control has its own copy of its UserControl, the `UserControl` of its [site](#the-usercontrol-a-program-sees), so that its controls are its own: a change to one control's copy leaves the UserControl's definition and the other placed controls as they are. A [`RDFormsFormView`](../api/RDCore.Forms.Avalonia.Rendering.RDFormsFormView.yml) inside it shows the copy, whose root takes the placed control's `Width` and `Height`.

|Setting|Behaviour|
|---|---|
|`BackStyle` 1, Opaque|The UserControl paints its `BackColor`, windowed or windowless.|
|`BackStyle` 0, Transparent|What is behind the UserControl shows where nothing is drawn, as VB6 shows a windowed one's form.|
|`Enabled` False|None of the UserControl's controls takes input.|
|`ControlContainer` True|The controls a form places in it are shown over the UserControl's own, placed from the UserControl's top left corner.|
|`InvisibleAtRuntime` True|Nothing is shown, and `Create` returns `null`.|
|`Windowless` True|The control is under the windowed controls of its container, as a Label is.|

A UserControl placed in another one is shown with its own controls. A UserControl that holds itself, at any depth, is shown once, with a box that names its class, such as `Project1.Gauge`, where it holds itself (RDForms' own).

A placed UserControl that is `Alignable`, on the form itself, is docked to the edge its `Align` names, as an [aligned PictureBox](containers.md#an-aligned-picturebox) is. It raises no Resize when it is.

A placed UserControl takes no focus, and Tab passes over it; a press on one of its controls gives that control the focus, and Tab from there goes to the form's next control. Its root and its controls raise their events to the window's sink, each with its component of the copy as the source.

> [!NOTE]
> **Not implemented.** A placed UserControl raises none of its own events (Initialize, ReadProperties, Resize, Show, Paint, Hide, Terminate) and none of its focus events, and its access keys do nothing. A program cannot move a control into one (425 in VB6 for one that is not a container).

## The UserControl a program sees

[`UserControlSite.Of`](../api/RDCore.Forms.Model.UserControlSite.yml) gives a placed UserControl's site: what the UserControl's code works with, one per placed control, an `InvisibleAtRuntime` one included.

|Member|What it is|
|---|---|
|`Extender`|The placed control.|
|`UserControl`|The placed control's own copy of its UserControl.|
|`Ambient`|The form's ambient properties, read from the form each time (a VBRUN `AmbientProperties`).|
|`PropertyBag`|The values the form's file keeps for the placed control, which `ReadProperties` reads (a VBRUN `PropertyBag`).|

### Ambient

|Member|Value|
|---|---|
|`BackColor`, `ForeColor`|The form's, unsigned: `&H8000000F` reads 2147483663.|
|`Font`|A copy of the form's `Font`, a new one at each read.|
|`RightToLeft`|False.|
|`ScaleUnits`|The name of the form's `ScaleMode`, such as `Twip` or `Pixel`.|
|`DisplayName`|The placed control's name.|
|`DisplayAsDefault`|The placed control's `Default`, where the UserControl's `DefaultCancel` is True; otherwise False.|
|`LocaleID`|The locale the program runs in.|

The other members have the values VB6 gives on a running form, such as `UserMode` True.

### PropertyBag

The bag holds the placed control's settings that are not the extender's, by name in any case. VB6 types each from the file's text:

|The file's text|The value read|
|---|---|
|A quoted string, such as `"007"`|A String.|
|A whole number of -32768 to 32767 without `&`, such as `42` or `-1 'True`|An Integer.|
|Another whole number a Long holds, or one written with `&`, such as `&H8000000F&`|A Long.|
|Another number a Currency holds, such as `1.5`|A Currency.|
|Any other number|A Double.|
|A `StdFont` bag (`BeginProperty Font {0BE35203-…}`)|A new `StdFont` of the bag's font, at each read.|
|`_ExtentX`, `_ExtentY`, where the UserControl's `InvisibleAtRuntime` is False|A Long: the placed control's width or height in whole pixels, in HIMETRIC units, whatever the file writes (133 pixels read 3519).|

`ReadProperty` of a name the bag does not hold, an extender property such as `Left` included, fails with 327 without a default; an empty name fails with 5. `WriteProperty` fails with 425 while the program runs.

> [!NOTE]
> **Not implemented.** A record of the companion file, such as `"Form1.frx":0000`, and a bag of another class are not in the bag. A form has no `RightToLeft`, so `Ambient.RightToLeft` reads False.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

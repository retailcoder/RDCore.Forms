# Importing MSForms (VBA UserForms)
<sup>_Ce document est disponible en [français](msforms.fr.md)_</sup>

`MSFormsFormReader` reads a VBA UserForm, as the VBE exports it, into the form model: the `.frm` file's designer block and the designer data its `OleObjectBlob` cites in the `.frx` file. RDCore.SDK reads the `.frx` file; the reader ports what it holds to RDForms' classes, members and units. It reads only: nothing writes a UserForm.

## Reading

|Method|Reads|
|---|---|
|`ReadFile(path, encoding)`|A `.frm` file, and the `.frx` file its `OleObjectBlob` cites from the same folder, the name compared without regard to case.|
|`Read(text, companion)`|A `.frm` file's text, and the bytes of its `.frx` file, or none.|
|`Read(text, design)`|A `.frm` file's text, and its designer data as RDCore.SDK's `MSFormsReader` read it.|

The text is decoded as Windows-1252 unless `ReadFile` is given another encoding. A cited name that is a rooted path, or that holds a NUL, is not read.

## The result

A read returns an `MSFormsFormImport`:

|Member|Holds|
|---|---|
|`Form`|The form, an `RDForms.Form`, or `null` when the file holds no UserForm.|
|`ModuleText`, `ModuleLine`|The code after the designer block (its `Attribute` lines included), and the line it starts on.|
|`Diagnostics`|What the import reports: the form's own, then its controls' in the order the form stores them.|

## Classes

Each MSForms class reads as the RDForms class the MSForms port map gives it: a `TextBox` is an `RDForms.TextBox`, and a `UserForm` an `RDForms.Form`. A `ScrollBar` is an `RDForms.HScrollBar` or an `RDForms.VScrollBar` by its `Orientation`; with `fmOrientationAuto`, a scroll bar wider than it is tall is horizontal. The controls a Frame or a page holds are placed in it.

## Values

|MSForms|RDForms|
|---|---|
|`Left`, `Top`, `Width`, `Height`, in points|The same, in pixels (1/96 inch, 0.75 point). The form's size is its client area's, from the `.frm` file's `ClientWidth` and `ClientHeight` in twips (15 a pixel).|
|`ControlTipText`|`ToolTipText`.|
|`TextAlign`|`Alignment`, with VB's values: left 0, right 1, centre 2.|
|A TextBox's `Value`, a ComboBox's `Value`|`Text`.|
|A ToggleButton|An `RDForms.ToggleButton`. Its `Value` is True where the form stores 1, and False where it stores 0 or nothing.|
|A SpinButton|An `RDForms.SpinButton`, with its `Value`, `Min`, `Max`, `SmallChange`, `Orientation` and `Delay`.|
|A MultiPage|An `RDForms.MultiPage`. Each of its pages is an `RDForms.Page`, with its tab's `Caption` and the controls it holds; a page fills its MultiPage, so its own size is not kept.|
|A TabStrip|An `RDForms.TabStrip`, with its `Tabs` in their order: each tab's name, its `Caption` with its `Accelerator`, its `ControlTipText` as `ToolTipText`, its `Tag`, `Enabled` and `Visible`.|
|`Caption` and `Accelerator`|`Caption`, with each literal `&` doubled and a `&` before the first letter that matches the `Accelerator`. A form's caption is kept as it is.|
|`SpecialEffect`|Flat and sunken are `Appearance` Flat and 3D. A Frame is drawn etched.|
|An Image's `PictureSizeMode`|Clip and stretch are `Stretch` False and True.|
|`Picture`, `MouseIcon`|The picture's bytes. A CommandButton with a picture is graphical (`Style` 1), as MSForms always shows its picture.|
|`BackColor`, `ForeColor`, `Font`, `MousePointer`, `Enabled`, `Visible`, `TabIndex`, `TabStop`, `Tag`, `Default`, `Cancel` and the members of the same name|Copied.|
|`ControlSource`, `RowSource`|Kept as text, on the same members. Nothing resolves them, and each one is reported.|

A value is set only where it differs from the RDForms class's default.

> [!NOTE]
> **Not implemented.** A metafile picture is kept and not drawn. `Zoom` and `IMEMode` are not implemented: a form or a control that sets one other than its default keeps it as text, with a diagnostic.

## What is kept, reported and left out

A read does not throw on what a file holds. A property the model does not have is kept as an `UnmodelledProperty` and not reported, as the VB6 reader does.

|Diagnostic|Severity|What the reader does|
|---|---|---|
|`NoDesignerBlock`, `NotAUserForm`|Error|No form.|
|`MalformedLine`|Warning|The line is left out.|
|`MissingCompanion`, `UnreadableDesignerData`|Warning|The form is read from its designer block alone, with no controls.|
|`UnmodelledClass`|Information|The control is kept as an `UnmodelledComponent`, with no text: its designer data is binary. The controls it holds are left out.|
|`InvalidValue`|Warning|A value the RDForms property cannot hold, such as a colour outside the OLE colours, is kept as text.|
|`ValueNotPorted`|Warning|A value with no RDForms equivalent, such as a raised `SpecialEffect`, a zoomed `PictureSizeMode`, a `GroupName`, or a CheckBox's or an OptionButton's picture, is kept as text.|
|`NotImplemented`|Warning|A `Zoom` or an `IMEMode` other than its default is kept as text.|
|`MetafilePicture`|Warning|The picture is kept and not drawn.|
|`AcceleratorNotInCaption`|Warning|The caption has no access key.|
|`UnresolvedDataSource`|Warning|The `ControlSource` or `RowSource` is kept as text. Its `Port` is the port diagnostic RDC40007.|

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](vb6.fr.md) • [IMPORT](vb6.md) | [CONTROLS](../controls/index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

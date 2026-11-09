# The RDForms document

An RDForms document holds one form as a constrained XAML dialect: XML whose elements are the form's components, named for their classes, and whose attributes are the properties they set. [`FormXaml`](../api/RDCore.Forms.Xaml.FormXaml.yml) reads a document into the model and writes the model as a document.

```xml
<Form FormatVersion="1" Name="Form1" Caption="Orders" Width="4800" Height="3600" xmlns="https://rubberduckvba.ca/schemas/rdforms">
  <Form.Font>
    <Font Name="Tahoma" Size="8.25" />
  </Form.Font>
  <Frame Name="Frame1" Caption="Ship by" Left="120" Top="120" Width="2400" Height="1200">
    <OptionButton Name="optShip" Index="0" Caption="&amp;Air" Value="True" Left="120" Top="240" Width="1800" Height="300" />
    <OptionButton Name="optShip" Index="1" Caption="&amp;Sea" Left="120" Top="600" Width="1800" Height="300" />
  </Frame>
  <ListBox Name="lstItems" Left="2640" Top="120" Width="2000" Height="1200">
    <ListBox.List>
      <Item Text="Alpha" ItemData="1" />
      <Item Text="Beta" ItemData="20" ItemIcon="R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==" />
    </ListBox.List>
  </ListBox>
  <CommandButton Name="cmdOK" Caption="OK" Left="3480" Top="2880" Width="1215" Height="495" />
</Form>
```

## Elements

Every element is in the namespace `https://rubberduckvba.ca/schemas/rdforms`.

|Element|Is|Holds|
|---|---|---|
|`Form`, `MDIForm`, `UserControl`, `PropertyPage`|The root: the form itself. Only the root has `FormatVersion`.|The form's components, and its property elements.|
|A control class, such as `CommandButton` or `Frame`|A component of that class of [`VBClasses`](../api/RDCore.Forms.Model.VB.VBClasses.yml).|The components placed in it, where its class is a container, and its property elements.|
|`Menu`|A menu item.|Its submenu's items.|
|`UnmodelledComponent`|A component the model does not have, such as an ActiveX control, kept as text: its `TypeName`, its `Name`, and its text as the element's content.|Text.|

A component that its parent cannot hold, such as a control written in a CommandButton, is placed in the nearest enclosing component that can.

## Attributes

|Attribute|Value|
|---|---|
|`FormatVersion`|The format's version, on the root: 1.|
|`Name`|The component's name. Every component has one.|
|`Index`|The component's index in its control array, from 0 to 32767.|
|A property, such as `Caption`|The value of a property the component's class has, compared without regard to case. A property the component does not set is left out, and has its class's default.|

|Value type|Written as|Example|
|---|---|---|
|String|The text.|`Caption="&amp;OK"`|
|Boolean|`True` or `False`.|`Enabled="False"`|
|Number|Invariant digits, with `.` for decimals.|`Left="120"`, `Size="8.25"`|
|Enumeration|The member's name; a number is read too.|`BorderStyle="FixedSingle"`|
|Colour|A system colour by its name, or `#RRGGBB`.|`BackColor="ButtonFace"`, `ForeColor="#1234AB"`|
|Picture|The bytes of its file, in base64.|`Picture="iVBORw0KGgo..."`|

Positions and sizes are in twips, as in the model: a control's `Left`, `Top`, `Width` and `Height` whatever its container's `ScaleMode`, and a Line's ends in its container's scale.

## Property elements

A property element is named for its component's element and the property, such as `ListBox.List`.

|Element|Holds|
|---|---|
|`<Class>.Font`|One `Font` element: `Name` and `Size`, and where they differ from the default, `Weight` (400 normal, 700 bold), `Italic`, `Underline`, `Strikethrough` and `Charset`.|
|`<Class>.List`|One `Item` for each item of a ListBox or ComboBox: its `Text`, and its `ItemData` and `ItemIcon` where it has them. An item with no `ItemData` has 0, and one with no `ItemIcon` has none.|
|`<Root>.ControlLibraries`|One `ControlLibrary` for each control library the form lists: its `Id`, `Version` and `FileName`.|
|`<Class>.UnmodelledProperties`|One `UnmodelledProperty` for each property the model does not have: its `Name` and `Text`.|
|`<Class>.<Property>`|Any other property's value as text, such as a caption of several lines.|

## What a document cannot hold

A document is read as data. It holds no code and names no handlers, and only the elements above are read: an Avalonia or WPF element, a markup extension, an `x:Class` or any other code-behind is not part of the format. The code behind a form handles its events by name, as in VB.

## Reading

[`FormXaml.Read`](../api/RDCore.Forms.Xaml.FormXaml.yml) reads a document's text, and `ReadFile` a file. A read does not throw on what a document holds: it keeps what the model cannot hold as text, and reports it in [`FormXamlRead.Diagnostics`](../api/RDCore.Forms.Xaml.FormXamlRead.yml).

|Diagnostic|Severity|What the reader does|
|---|---|---|
|`MalformedDocument`|Error|No form: the document is not well-formed XML, declares a DTD, or is longer than 67,108,864 characters.|
|`NotAForm`|Error|No form: the root element is not a root class in the format's namespace.|
|`NamelessComponent`|Warning (Error for the root)|The component is kept as an `UnmodelledComponent`.|
|`NestingTooDeep`|Warning|A component nested deeper than 2,500 (the root included) is kept as an `UnmodelledComponent`, with its contents.|
|`MisplacedComponent`|Warning|The component is placed in the nearest enclosing component that can hold it.|
|`UnknownProperty`|Warning|The value is kept as an `UnmodelledProperty`.|
|`InvalidValue`|Warning|The value is kept as an `UnmodelledProperty`: one its property's type cannot hold, a picture that is not base64 or is larger than 16 MiB, or an `Index` outside 0 to 32767.|
|`NewerFormatVersion`|Information|The document is read anyway.|
|`UnmodelledElement`|Information|An element the format does not have in its place is kept as an `UnmodelledComponent`, with its XML as its text.|

A document's own XML is read safely: a DTD is refused, and nothing outside the document is read.

## Writing

`FormXaml.Write` returns a form's document, indented; `WriteFile` writes it to a file as UTF-8 with an XML declaration. A document written and read again holds the same form, except for the `ItemData` and `ItemIcon` values past a list's last item, which are not written. Line breaks and tabs in a string are written so that they are read back as they were. A character XML 1.0 does not allow, such as a NUL, is written as a character reference, which `Read` reads back and another XML reader may refuse.

## Versions

The format's version is 1. A document of a later version is read anyway, with a `NewerFormatVersion` report, and what this version does not know is reported as it is met.

## Implementation

|Type|Role|
|---|---|
|[`FormXaml`](../api/RDCore.Forms.Xaml.FormXaml.yml)|Reads and writes documents.|
|[`FormXamlRead`](../api/RDCore.Forms.Xaml.FormXamlRead.yml)|The form a read returns, its format version, and its diagnostics.|
|[`FormXamlDiagnostic`](../api/RDCore.Forms.Xaml.FormXamlDiagnostic.yml)|One thing a read reports: its kind, severity, line and message.|

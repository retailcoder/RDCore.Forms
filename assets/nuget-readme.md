# RDCore.Forms

RDForms, the form toolkit of [RDCore](https://github.com/rubberduck-vba/RDCore), a language platform for VBA. It describes forms, their controls and their events, imports Visual Basic 6.0 and MSForms (VBA UserForm) forms, and shows them with [Avalonia](https://avaloniaui.net) on Windows, Linux and macOS.

**This is an alpha.** The API will change before 1.0, and a later alpha may break code built against an earlier one.

## What the package holds

| Assembly | What it is |
|---|---|
| `RDCore.Forms` | The form model, with no UI dependency: component classes, their properties and events, the components of a form, the reader and writer of the RDForms document, and the readers that import a VB6 or MSForms form into the model. |
| `RDCore.Forms.Avalonia` | The controls of a form, and the window built from the model, rendered with Avalonia. |

`RDCore.Forms` carries its French resources (`fr`) beside the English ones.

## Getting started

```bash
dotnet add package RDCore.Forms --prerelease
```

The package targets .NET 11 (`net11.0`). It depends on Avalonia and on RDCore.SDK.

## Documentation

- [Documentation site](https://rubberduck-vba.github.io/RDCore.Forms/): getting started, importing forms, the controls, and the RDForms document format.
- [API reference](https://rubberduck-vba.github.io/RDCore.Forms/api/RDCore.Forms.Model.html), generated from the xmldocs of the package's assemblies.
- [Source and issues](https://github.com/rubberduck-vba/RDCore.Forms).

## Licence

MIT. The licence text is in the package and in the [repository](https://github.com/rubberduck-vba/RDCore.Forms/blob/main/LICENSE). A French version of the licence is available on request. The package's NOTICE.md names the third-party code it includes, with its licence.

The RDCore platform that hosts RDForms, the RD-VBA runtime included, is licensed under the GNU GPL v3 or later, in the [RDCore](https://github.com/rubberduck-vba/RDCore) repository.

## Contributing

Contributions are open to individuals, under the [CLA](https://github.com/rubberduck-vba/RDCore.Forms/blob/main/CLA.md). One signature covers every repository of the RDCore platform.

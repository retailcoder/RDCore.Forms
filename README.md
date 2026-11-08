# RDCore.Forms
<sup>_Ce document est disponible en [français](./README.fr.md)_</sup>

![VIVAT CUCUMIS](./assets/vivat-cucumis-stonecore.png)

RDCore.Forms is RDForms, the form toolkit of the [RDCore](https://github.com/rubberduck-vba/RDCore) platform. It describes forms, their controls and their events, renders them with [Avalonia](https://avaloniaui.net) on Windows, Linux and macOS, and imports the forms of Visual Basic 6.0 projects (`.frm` and `.frx` files) and of VBA projects (UserForms).

The RDCore runtime runs the code behind a form. RDCore.Forms shows the form and raises its controls' events to that code.

This repository holds two libraries:

| Package | Role |
|---|---|
| **RDCore.Forms** | The form model, with no UI dependency: component classes, their properties and events, the instances of a form, and the readers that import a form into the model. The language server reads and analyses a form with it, without loading Avalonia. |
| **RDCore.Forms.Avalonia** | The controls and the renderer: the controls of a form, and the window built from the model. |

## Project status

RDCore.Forms is in **pre-alpha** development. The repository is being set up, and no package is published yet.

[![Build and Test](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml/badge.svg)](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml)

## Building

From the repository root, with the .NET 10 SDK:

```
dotnet build RDCore.Forms.slnx
dotnet test RDCore.Forms.slnx
```

The control tests run on Avalonia's headless platform, so they need no display.

## Licence

RDCore.Forms is licensed under the **MIT** licence: see [LICENSE](LICENSE). Files derived from [HexIDE](https://github.com/hexide-io/HexIDE) keep HexIDE's copyright notice: see [NOTICE.md](NOTICE.md).

## Contributions

- Individuals: open, under the [RDCore CLA](https://github.com/rubberduck-vba/RDCore/blob/main/CLA.md). One signature covers every RDCore repository.
- Corporate: planned.

## See also

- [RDCore](https://github.com/rubberduck-vba/RDCore): the platform, its runtime and its language server
- [RDCore.SDK](https://github.com/rubberduck-vba/RDCore.SDK): the language model and platform plumbing that every RDCore component and extension builds on
- [RDCore.HexIDE](https://github.com/rubberduck-vba/RDCore.HexIDE): the add-in that brings RDCore into HexIDE
- [HexIDE](https://github.com/hexide-io/HexIDE): the IDE for VB developers that RDCore.HexIDE extends

<hr/>
<h6 align='center'>V I V A T ❤️ C U C U M I S ™</h6>
<p align='center' style='font-size:8pt;'>
<small>© Copyright <strong>9562-7303 Québec inc.</strong> (2026)<br/><em>Seul, &quot;Rubberduck&quot; est utilisé pour fins de référence au projet open-source legacy <strong>utilisé publiquement ainsi depuis 2015</strong> et sans lien ni affiliation avec tout tiers détenteur d'une marque semblable dans quelque juridiction que ce soit.<br/>&quot;Rubberduck VBA&quot;, &quot;RDCore&quot; et &quot;VIVAT CUCUMIS&quot; sont des marques de commerce revendiquées par 9562-7303 Québec inc. (en attente); Toutes les marques appartiennent à leur détenteur respectif.<br/>RDCore n'est pas un produit de Microsoft et n'est pas affilié à Microsoft, ni directement, ni indirectement.</em><br/><br/><em>If used alone, &quot;Rubberduck&quot; is used as a reference to the legacy open-source project <strong>the same way it has been used publicly since 2015</strong> and without any links or affiliation with any third-party trademark holders of a similar trademark in any jurisdiction.<br/>&quot;Rubberduck VBA&quot;, &quot;RDCore&quot; and &quot;VIVAT CUCUMIS&quot; are trademarks claimed by 9562-7303 Québec inc. (pending). All trademarks belong to their respective owners.<br/>RDCore is not a Microsoft product and is not affiliated with Microsoft, directly or indirectly.</em></small>
</p>

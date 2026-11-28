# RDCore.Forms
<sup>_Ce document est disponible en [français](./README.fr.md)_</sup>

![VIVAT CUCUMIS](./assets/vivat-cucumis-stonecore.png)
<h6 align='center'>V I V A T ❤️ C U C U M I S ™</h6>
<p align='center'><small><a href='https://rubberduckvba.ca'>rubberduckvba.ca</a></small></p>
<hr/>

## Overview

RDCore.Forms is RDForms, the form toolkit of the [RDCore](https://github.com/rubberduck-vba/RDCore) platform. It describes forms, their controls and their events, and imports the forms of Visual Basic 6.0 projects (`.frm` and `.frx` files). Its renderer shows those forms with [Avalonia](https://avaloniaui.net) on Windows, Linux and macOS, and the RDCore runtime runs the code behind them. The package holds two assemblies: **RDCore.Forms**, the form model with no UI dependency, and **RDCore.Forms.Avalonia**, the controls and the renderer.

[![NuGet](https://img.shields.io/nuget/vpre/RDCore.Forms)](https://www.nuget.org/packages/RDCore.Forms)
[![Build and Test](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml/badge.svg)](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml)
[![Coverage](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/rubberduck-vba/RDCore.Forms/badges/coverage.json)](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml)

## Installation

The package is on NuGet. Each version is an **alpha** prerelease: the API will change before 1.0.

```
dotnet add package RDCore.Forms --prerelease
```

It targets .NET 11 (`net11.0`) and depends on RDCore.SDK and Avalonia.

## Building from source

From the repository root, with the .NET 11 SDK:

```
dotnet build RDCore.Forms.slnx
dotnet test RDCore.Forms.slnx
```

The tests run in a headless Avalonia session, so they need no display.

## Documentation

- [Introduction](https://rubberduck-vba.github.io/RDCore.Forms/introduction.html)
- [Getting started](https://rubberduck-vba.github.io/RDCore.Forms/getting-started.html)
- [Import](https://rubberduck-vba.github.io/RDCore.Forms/import/vb6.html)
- [Controls](https://rubberduck-vba.github.io/RDCore.Forms/controls/index.html)
- [Format](https://rubberduck-vba.github.io/RDCore.Forms/format/index.html)
- [API](https://rubberduck-vba.github.io/RDCore.Forms/api/RDCore.Forms.Avalonia.html)

## See also

- [The RDCore.Forms documentation](https://rubberduck-vba.github.io/RDCore.Forms/)
- [RDCore](https://github.com/rubberduck-vba/RDCore): the platform, its runtime and its language server
- [RDCore.SDK](https://github.com/rubberduck-vba/RDCore.SDK): the language model and platform plumbing that every RDCore component and extension builds on
- [RDCore.HexIDE](https://github.com/rubberduck-vba/RDCore.HexIDE): the add-in that brings RDCore into HexIDE
- [HexIDE](https://github.com/hexide-io/HexIDE): the IDE for VB developers that RDCore.HexIDE extends
- [Code of Conduct](https://github.com/rubberduck-vba/RDCore/blob/main/CODE_OF_CONDUCT.md)
- [Contributing](https://github.com/rubberduck-vba/RDCore/blob/main/CONTRIBUTING.md): open to individuals under the [RDCore CLA](https://github.com/rubberduck-vba/RDCore/blob/main/CLA.md), one signature for every RDCore repository; corporate contributions are planned

> [!IMPORTANT]
> **License:** RDCore.Forms is licensed under the **MIT** license ([LICENSE](LICENSE)). Files derived from [HexIDE](https://github.com/hexide-io/HexIDE) keep HexIDE's copyright notice: see [NOTICE.md](NOTICE.md).

<hr/>
<p align='center'><img alt='Logo™ 9562-7303 Québec inc.' src='./assets/vector-ducky.svg' style='width:200px; align:center;' /></p>
<h6 align='center'>V I V A T ❤️ C U C U M I S ™</h6>
<p align='center' style='font-size:8pt;'>
<small>© Copyright <strong>9562-7303 Québec inc.</strong> (2026)<br/><em>Seul, &quot;Rubberduck&quot; est utilisé pour fins de référence au projet open-source legacy <strong>utilisé publiquement ainsi depuis 2015</strong> et sans lien ni affiliation avec tout tiers détenteur d'une marque semblable dans quelque juridiction que ce soit.<br/>&quot;Rubberduck VBA&quot;, &quot;RDCore&quot; et &quot;VIVAT CUCUMIS&quot; sont des marques de commerce revendiquées par 9562-7303 Québec inc. (en attente); Toutes les marques appartiennent à leur détenteur respectif.<br/>RDCore.Forms n'est pas un produit de Microsoft et n'est pas affilié à Microsoft, ni directement, ni indirectement.</em><br/><br/><em>If used alone, &quot;Rubberduck&quot; is used as a reference to the legacy open-source project <strong>the same way it has been used publicly since 2015</strong> and without any links or affiliation with any third-party trademark holders of a similar trademark in any jurisdiction.<br/>&quot;Rubberduck VBA&quot;, &quot;RDCore&quot; and &quot;VIVAT CUCUMIS&quot; are trademarks claimed by 9562-7303 Québec inc. (pending). All trademarks belong to their respective owners.<br/>RDCore.Forms is not a Microsoft product and is not affiliated with Microsoft, directly or indirectly.</em></small>
</p>

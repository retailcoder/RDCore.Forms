# AVIS
<sup>_[English version](#notice) follows_</sup>

**RDCore.Forms** © Copyright **9562-7303 Québec inc.** (2026)

Ce fichier accompagne le code source de RDCore.Forms et chaque paquet publié à partir de celui-ci.

## Licence

Le contenu de ce référentiel est distribué sous la licence **MIT** : voir [LICENSE](LICENSE). Le fichier de licence reproduit le texte officiel de la licence, en anglais. Une version française est disponible sur demande.

## Code tiers

Des parties de RDCore.Forms sont dérivées de [HexIDE](https://github.com/hexide-io/HexIDE), à partir du _commit_ `ae17f1db` : le modèle des composants, les contrôles Avalonia et la lecture des fichiers `.frm` et `.frx`.

- HexIDE est distribué sous la licence MIT.
- Chaque fichier dérivé de HexIDE conserve, dans son en-tête, l'avis de droit d'auteur de HexIDE.
- Les formulaires de test du dossier `tests/RDCore.Forms.Tests/Import/VB6/Fixtures/HexIDE` sont copiés tels quels du corpus de HexIDE, au _commit_ `ae17f1db`, sous la même licence.
- La licence de HexIDE est reproduite à la fin de ce fichier, dans sa version anglaise originale, comme elle l'exige : voir [Licence de HexIDE](#hexide-licence).

Les paquets NuGet dont dépend RDCore.Forms, dont Avalonia, demeurent sous leurs propres licences.

## Palette sombre de WinUI 3

La palette sombre de `FormPalette` (`src/RDCore.Forms.Avalonia/Controls/FormPalette.cs`) reprend les couleurs du thème sombre de WinUI 3, dans le fichier `controls/dev/CommonStyles/Common_themeresources_any.xaml` de [microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml), au _commit_ `8463f451`.

- microsoft-ui-xaml est distribué sous la licence MIT.
- La licence de microsoft-ui-xaml est reproduite à la fin de ce fichier, dans sa version anglaise originale, comme elle l'exige : voir [Licence de microsoft-ui-xaml](#microsoft-ui-xaml-licence).

## Description de VB6.OLB

Le fichier `src/RDCore.Forms/Libraries/VB.json` est la sortie de `rdc export-library` pour VB6.OLB : il décrit les classes, les membres et les événements que déclare cette bibliothèque de types. Il est distribué sous la licence MIT, comme le reste de RDCore.Forms. VB6.OLB lui-même n'est pas inclus.

## Description de MSCOMCTL.OCX

Le fichier `src/RDCore.Forms/Libraries/MSComctlLib.json` est la sortie de `rdc export-library` pour MSCOMCTL.OCX (Microsoft Windows Common Controls 6.0) : il décrit les classes, les membres, les événements et les énumérations que déclare sa bibliothèque de types. Il est distribué sous la licence MIT, comme le reste de RDCore.Forms. MSCOMCTL.OCX lui-même n'est pas inclus.

## Description de FM20.DLL

Le fichier `src/RDCore.Forms/Libraries/MSForms.json` est la sortie de `rdc export-library` pour FM20.DLL (Microsoft Forms 2.0 Object Library) : il décrit les classes, les membres, les événements et les énumérations que déclare sa bibliothèque de types. Il est distribué sous la licence MIT, comme le reste de RDCore.Forms. FM20.DLL lui-même n'est pas inclus.

## Marques de commerce

Seul, "Rubberduck" est utilisé pour fins de référence au projet open-source legacy **utilisé publiquement ainsi depuis 2015** et sans lien ni affiliation avec tout tiers détenteur d'une marque semblable dans quelque juridiction que ce soit.  
"Rubberduck VBA", "RDCore" et "VIVAT CUCUMIS" sont des marques de commerce revendiquées par 9562-7303 Québec inc. (en attente); Toutes les marques appartiennent à leur détenteur respectif.  
RDCore.Forms n'est pas un produit de Microsoft et n'est pas affilié à Microsoft, ni directement, ni indirectement.


---

# NOTICE
<sup>_[Version française](#avis) ci-haut_</sup>

**RDCore.Forms** © Copyright **9562-7303 Québec inc.** (2026)

This file accompanies the RDCore.Forms source code and every package published from it.

## Licence

The content of this repository is licensed under the **MIT** licence: see [LICENSE](LICENSE). The licence file reproduces the official text of the licence, in English. A French version is available on request.

## Third-party code

Parts of RDCore.Forms are derived from [HexIDE](https://github.com/hexide-io/HexIDE), from commit `ae17f1db`: the component model, the Avalonia controls, and the reading of `.frm` and `.frx` files.

- HexIDE is licensed under the MIT licence.
- Every file derived from HexIDE keeps HexIDE's copyright notice in its header.
- The test forms in `tests/RDCore.Forms.Tests/Import/VB6/Fixtures/HexIDE` are copied as is from HexIDE's corpus, at commit `ae17f1db`, under the same licence.
- HexIDE's licence is reproduced below, as it requires.

The NuGet packages RDCore.Forms depends on, Avalonia among them, remain under their own licences.

## WinUI 3 dark palette

The dark palette of `FormPalette` (`src/RDCore.Forms.Avalonia/Controls/FormPalette.cs`) takes the colours of WinUI 3's dark theme, from the file `controls/dev/CommonStyles/Common_themeresources_any.xaml` of [microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml), at commit `8463f451`.

- microsoft-ui-xaml is licensed under the MIT licence.
- microsoft-ui-xaml's licence is reproduced below, as it requires.

## Description of VB6.OLB

The file `src/RDCore.Forms/Libraries/VB.json` is `rdc export-library` output from VB6.OLB: it describes the classes, members and events that type library declares. It is licensed under the MIT licence, like the rest of RDCore.Forms. VB6.OLB itself is not included.

## Description of MSCOMCTL.OCX

The file `src/RDCore.Forms/Libraries/MSComctlLib.json` is `rdc export-library` output from MSCOMCTL.OCX (Microsoft Windows Common Controls 6.0): it describes the classes, members, events and enumerations its type library declares. It is licensed under the MIT licence, like the rest of RDCore.Forms. MSCOMCTL.OCX itself is not included.

## Description of FM20.DLL

The file `src/RDCore.Forms/Libraries/MSForms.json` is `rdc export-library` output from FM20.DLL (Microsoft Forms 2.0 Object Library): it describes the classes, members, events and enumerations its type library declares. It is licensed under the MIT licence, like the rest of RDCore.Forms. FM20.DLL itself is not included.

## Trademarks

If used alone, "Rubberduck" is used as a reference to the legacy open-source project **the same way it has been used publicly since 2015** and without any links or affiliation with any third-party trademark holders of a similar trademark in any jurisdiction.  
"Rubberduck VBA", "RDCore" and "VIVAT CUCUMIS" are trademarks claimed by 9562-7303 Québec inc. (pending). All trademarks belong to their respective owners.  
RDCore.Forms is not a Microsoft product and is not affiliated with Microsoft, directly or indirectly.

---

## HexIDE licence

```
MIT License

Copyright (c) 2024 Bartosz Korczyński
Copyright (c) 2026 The HexIDE Authors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## microsoft-ui-xaml licence

```
MIT License

Copyright (c) Microsoft Corporation. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE
```

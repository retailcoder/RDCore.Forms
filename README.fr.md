# RDCore.Forms
<sup>_This document is available in [English](./README.md)_</sup>

![VIVAT CUCUMIS](./assets/vivat-cucumis-stonecore.png)
<h6 align='center'>V I V A T ❤️ C U C U M I S ™</h6>
<p align='center'><small><a href='https://rubberduckvba.ca'>rubberduckvba.ca</a></small></p>
<hr/>

## Aperçu

RDCore.Forms est RDForms, la bibliothèque de formulaires de la plateforme [RDCore](https://github.com/rubberduck-vba/RDCore). Elle décrit les formulaires, leurs contrôles et leurs événements, et importe les formulaires des projets Visual Basic 6.0 (fichiers `.frm` et `.frx`). Son moteur de rendu affiche ces formulaires avec [Avalonia](https://avaloniaui.net) sous Windows, Linux et macOS, et le _runtime_ de RDCore exécute le code qui les accompagne. Le paquet contient deux assemblages : **RDCore.Forms**, le modèle des formulaires sans dépendance d'interface, et **RDCore.Forms.Avalonia**, les contrôles et le rendu.

[![NuGet](https://img.shields.io/nuget/vpre/RDCore.Forms)](https://www.nuget.org/packages/RDCore.Forms)
[![Build and Test](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml/badge.svg)](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml)

## Installation

Le paquet est sur NuGet. Chaque version est une préversion **alpha** : l'API changera avant la version 1.0.

```
dotnet add package RDCore.Forms --prerelease
```

Il cible .NET 11 (`net11.0`) et dépend de RDCore.SDK et d'Avalonia.

## Compiler à partir des sources

À partir de la racine du dépôt, avec le SDK .NET 11 :

```
dotnet build RDCore.Forms.slnx
dotnet test RDCore.Forms.slnx
```

Les tests s'exécutent dans une session _headless_ d'Avalonia, sans affichage.

## Documentation

- [Introduction](https://rubberduck-vba.github.io/RDCore.Forms/introduction.fr.html)
- [Premiers pas](https://rubberduck-vba.github.io/RDCore.Forms/getting-started.fr.html)
- [Import](https://rubberduck-vba.github.io/RDCore.Forms/import/vb6.html)
- [Controls](https://rubberduck-vba.github.io/RDCore.Forms/controls/index.html)
- [Format](https://rubberduck-vba.github.io/RDCore.Forms/format/index.html)
- [API](https://rubberduck-vba.github.io/RDCore.Forms/api/RDCore.Forms.Avalonia.html)

## Voir aussi

- [La documentation de RDCore.Forms](https://rubberduck-vba.github.io/RDCore.Forms/index.fr.html)
- [RDCore](https://github.com/rubberduck-vba/RDCore) : la plateforme, son _runtime_ et son serveur de langage
- [RDCore.SDK](https://github.com/rubberduck-vba/RDCore.SDK) : le modèle du langage et l'infrastructure commune sur lesquels reposent chaque composant et chaque extension de RDCore
- [RDCore.HexIDE](https://github.com/rubberduck-vba/RDCore.HexIDE) : le module d'extension qui intègre RDCore à HexIDE
- [HexIDE](https://github.com/hexide-io/HexIDE) : l'environnement de développement pour les développeurs VB que RDCore.HexIDE étend
- [Code de conduite](https://github.com/rubberduck-vba/RDCore/blob/main/CODE_OF_CONDUCT.fr.md)
- [Contribuer](https://github.com/rubberduck-vba/RDCore/blob/main/CONTRIBUTING.md) : ouvert aux individus selon le [CLA de RDCore](https://github.com/rubberduck-vba/RDCore/blob/main/CLA.fr.md), une seule signature pour tous les dépôts de RDCore; les contributions corporatives sont prévues

> [!IMPORTANT]
> **Licence :** RDCore.Forms est distribué sous la licence **MIT** ([LICENSE](LICENSE)). Les fichiers dérivés de [HexIDE](https://github.com/hexide-io/HexIDE) conservent l'avis de droit d'auteur de HexIDE : voir [NOTICE.md](NOTICE.md).

<hr/>
<p align='center'><img alt='Logo™ 9562-7303 Québec inc.' src='./assets/vector-ducky.svg' style='width:200px; align:center;' /></p>
<h6 align='center'>V I V A T ❤️ C U C U M I S ™</h6>
<p align='center' style='font-size:8pt;'>
<small>© Copyright <strong>9562-7303 Québec inc.</strong> (2026)<br/><em>Seul, &quot;Rubberduck&quot; est utilisé pour fins de référence au projet open-source legacy <strong>utilisé publiquement ainsi depuis 2015</strong> et sans lien ni affiliation avec tout tiers détenteur d'une marque semblable dans quelque juridiction que ce soit.<br/>&quot;Rubberduck VBA&quot;, &quot;RDCore&quot; et &quot;VIVAT CUCUMIS&quot; sont des marques de commerce revendiquées par 9562-7303 Québec inc. (en attente); Toutes les marques appartiennent à leur détenteur respectif.<br/>RDCore.Forms n'est pas un produit de Microsoft et n'est pas affilié à Microsoft, ni directement, ni indirectement.</em><br/><br/><em>If used alone, &quot;Rubberduck&quot; is used as a reference to the legacy open-source project <strong>the same way it has been used publicly since 2015</strong> and without any links or affiliation with any third-party trademark holders of a similar trademark in any jurisdiction.<br/>&quot;Rubberduck VBA&quot;, &quot;RDCore&quot; and &quot;VIVAT CUCUMIS&quot; are trademarks claimed by 9562-7303 Québec inc. (pending). All trademarks belong to their respective owners.<br/>RDCore.Forms is not a Microsoft product and is not affiliated with Microsoft, directly or indirectly.</em></small>
</p>

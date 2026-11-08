# RDCore.Forms
<sup>_This document is available in [English](./README.md)_</sup>

![VIVAT CUCUMIS](./assets/vivat-cucumis-stonecore.png)

**RDForms** est la boîte à outils de formulaires de la plateforme [RDCore](https://github.com/rubberduck-vba/RDCore) : des formulaires multiplateformes pour RD-VBA, construits sur [Avalonia](https://avaloniaui.net) et conçus pour MVVM. Elle importe les formulaires des projets VB6 (`.frm` + `.frx`) et les UserForms des projets VBA (MSForms), pour que leur code s'exécute avec les mêmes contrôles sous Windows, Linux et macOS.

Ce dépôt contient deux bibliothèques :

| Paquet | Rôle |
|---|---|
| **RDCore.Forms** | Le modèle des formulaires, sans dépendance d'interface : les classes de composants, leurs propriétés et leurs événements, les instances d'un formulaire, et les lecteurs qui importent un formulaire dans le modèle. Le serveur de langage lit et analyse un formulaire avec elle, sans charger Avalonia. |
| **RDCore.Forms.Avalonia** | Les contrôles et le rendu : les contrôles d'un formulaire, et la fenêtre construite à partir du modèle. |

Le _runtime_ de RDCore exécute le code du formulaire. Le processus hôte qui affiche un formulaire pour le _runtime_ fait partie de RDCore.

## État du projet

RDCore.Forms est en développement **pré-alpha**. Rien n'est encore publié : il n'y a ni paquet ni version, et l'API changera.

**Contributions**

- Individus : ouvertes ([CLA](CLA.fr.md)). Une seule signature couvre tous les dépôts RDCore.
- Corporatives : prévues.

[![Build and Test](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml/badge.svg)](https://github.com/rubberduck-vba/RDCore.Forms/actions/workflows/build.yml)

## Licence

RDCore.Forms est distribué sous licence **MIT** (voir [LICENSE](LICENSE)). Une partie de son code provient de [HexIDE](https://github.com/hexide-io/HexIDE), également sous MIT : [NOTICE.md](NOTICE.md) en nomme la source et en reproduit la licence.

## Compiler

À partir de la racine du dépôt, avec le SDK .NET 10 :

```
dotnet build RDCore.Forms.slnx
dotnet test RDCore.Forms.slnx
```

Les tests des contrôles s'exécutent sur la plateforme _headless_ d'Avalonia, sans affichage.

---
### Voir aussi
- [RDCore](https://github.com/rubberduck-vba/RDCore)
- [RDCore.SDK](https://github.com/rubberduck-vba/RDCore.SDK)
- [HexIDE](https://github.com/hexide-io/HexIDE)

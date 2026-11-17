# Démarrer
<sup>_This document is available in [English](getting-started.md)_</sup>

Un projet utilise RDCore.Forms à partir de son paquet, ou de ses sources.

## Installer

Le paquet RDCore.Forms contient le modèle (l'assemblage `RDCore.Forms`) ainsi que les contrôles et la fenêtre (l'assemblage `RDCore.Forms.Avalonia`). Il dépend d'Avalonia et de RDCore.SDK. C'est une préversion :

```
dotnet add package RDCore.Forms --prerelease
```

## Compiler à partir des sources

À partir de la racine du [référentiel](https://github.com/rubberduck-vba/RDCore.Forms), avec le SDK .NET 11 :

```
dotnet build RDCore.Forms.slnx
dotnet test RDCore.Forms.slnx
```

Les tests s'exécutent dans une session _headless_ d'Avalonia, sans affichage.

## Référencer les sources

Un projet compilé à partir des sources référence `src/RDCore.Forms.Avalonia/RDCore.Forms.Avalonia.csproj`, qui apporte le modèle avec lui, comme le paquet :

```xml
<ItemGroup>
  <ProjectReference Include="..\RDCore.Forms\src\RDCore.Forms.Avalonia\RDCore.Forms.Avalonia.csproj" />
</ItemGroup>
```

## Lire un formulaire

[`VB6FormReader.ReadFile`](api/RDCore.Forms.Import.VB6.VB6FormReader.yml) lit un fichier de conception Visual Basic 6.0, et le fichier binaire associé qu'il cite :

```csharp
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;

var import = VB6FormReader.ReadFile("Form1.frm");

foreach (var diagnostic in import.Diagnostics)
{
    Console.WriteLine(diagnostic);
}

if (import.Form is { } form)
{
    foreach (var component in form.Components)
    {
        Console.WriteLine($"{component} ({component.Class.TypeName})");
    }

    Console.WriteLine(form.Find("Command1")?.Get(Properties.Caption));
}
```

[Importer des formulaires VB6](import/vb6.fr.md) décrit ce que le lecteur lit, ce qu'il conserve sous forme de texte, et ce qu'il signale.

---
[ACCUEIL](index.fr.md) • [HOME](index.md) | [BIENVENUE](introduction.fr.md) • [WELCOME](introduction.md) | DÉMARRER • [GET STARTED](getting-started.md) | [IMPORTER](import/vb6.fr.md) • [IMPORT](import/vb6.md) | [CONTROLS](controls/index.md) | [FORMAT](format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

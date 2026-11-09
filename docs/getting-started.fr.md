# Démarrer
<sup>_This document is available in [English](getting-started.md)_</sup>

Aucun paquet n'est publié : un projet utilise donc RDCore.Forms à partir de ses sources.

## Compiler

À partir de la racine du [référentiel](https://github.com/rubberduck-vba/RDCore.Forms), avec le SDK .NET 10 :

```
dotnet build RDCore.Forms.slnx
dotnet test RDCore.Forms.slnx
```

Les tests s'exécutent dans une session _headless_ d'Avalonia, sans affichage.

## Référencer le modèle

Un projet qui lit des formulaires référence `src/RDCore.Forms/RDCore.Forms.csproj` :

```xml
<ItemGroup>
  <ProjectReference Include="..\RDCore.Forms\src\RDCore.Forms\RDCore.Forms.csproj" />
</ItemGroup>
```

## Lire un formulaire

[`VB6FormReader.ReadFile`](api/RDCore.Forms.Import.VB6.VB6FormReader.yml) lit un fichier de conception Visual Basic 6.0, et le fichier binaire associé qu'il cite :

```csharp
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model.VB;

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

    Console.WriteLine(form.Find("Command1")?.Get(VBProperties.Caption));
}
```

[Importer des formulaires VB6](import/vb6.fr.md) décrit ce que le lecteur lit, ce qu'il conserve sous forme de texte, et ce qu'il signale.

---
[ACCUEIL](index.fr.md) • [HOME](index.md) | [BIENVENUE](introduction.fr.md) • [WELCOME](introduction.md) | DÉMARRER • [GET STARTED](getting-started.md) | [IMPORTER](import/vb6.fr.md) • [IMPORT](import/vb6.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

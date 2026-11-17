# Getting started
<sup>_Ce document est disponible en [français](getting-started.fr.md)_</sup>

A project uses RDCore.Forms from its package, or from its source.

## Installing

The RDCore.Forms package holds the model (the `RDCore.Forms` assembly), and the controls and the window (the `RDCore.Forms.Avalonia` assembly). It depends on Avalonia and on RDCore.SDK. It is a prerelease:

```
dotnet add package RDCore.Forms --prerelease
```

## Building from source

From the root of the [repository](https://github.com/rubberduck-vba/RDCore.Forms), with the .NET 11 SDK:

```
dotnet build RDCore.Forms.slnx
dotnet test RDCore.Forms.slnx
```

The tests run in a headless Avalonia session, so they need no display.

## Referencing the source

A project built against the source references `src/RDCore.Forms.Avalonia/RDCore.Forms.Avalonia.csproj`, which brings the model with it, as the package does:

```xml
<ItemGroup>
  <ProjectReference Include="..\RDCore.Forms\src\RDCore.Forms.Avalonia\RDCore.Forms.Avalonia.csproj" />
</ItemGroup>
```

## Reading a form

[`VB6FormReader.ReadFile`](api/RDCore.Forms.Import.VB6.VB6FormReader.yml) reads a Visual Basic 6.0 designer file, and the companion binary file it cites:

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

[Importing VB6 forms](import/vb6.md) says what the reader reads, what it keeps as text, and what it reports.

---
[ACCUEIL](index.fr.md) • [HOME](index.md) | [BIENVENUE](introduction.fr.md) • [WELCOME](introduction.md) | [DÉMARRER](getting-started.fr.md) • GET STARTED | [IMPORTER](import/vb6.fr.md) • [IMPORT](import/vb6.md) | [CONTROLS](controls/index.md) | [FORMAT](format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

# Importing VB6 forms
<sup>_Ce document est disponible en [français](vb6.fr.md)_</sup>

[`VB6FormReader`](../api/RDCore.Forms.Import.VB6.VB6FormReader.yml) reads the designer files of a Visual Basic 6.0 project into the form model: forms (`.frm`), user controls (`.ctl`) and property pages (`.pag`), with the companion binary files they cite (`.frx`, `.ctx`, `.pgx`). It reads only: nothing writes a designer file.

## Reading

|Method|Reads|
|---|---|
|`ReadFile(path, options)`|A designer file, and each companion file it cites, from the designer file's folder, the name compared without regard to case.|
|`Read(text, companion, options)`|A designer file's text, and the bytes of its companion file, or none.|

The text is decoded as Windows-1252 unless [`VB6ImportOptions.Encoding`](../api/RDCore.Forms.Import.VB6.VB6ImportOptions.yml) says otherwise.

`ReadFile` reads a companion only from the designer file's folder. A cited name that is a rooted path, such as a network path, is not read, and neither is a name that holds a NUL: each is reported as a missing companion.

## The result

A read returns a [`VB6FormImport`](../api/RDCore.Forms.Import.VB6.VB6FormImport.yml):

|Member|Holds|
|---|---|
|`Form`|The form, or `null` when the file holds none.|
|`ModuleText`, `ModuleLine`|The code after the designer half (its `Attribute` lines included), and the line it starts on.|
|`UnmodelledHeader`|The header lines that name no control library the form lists, as they were read.|
|`UnmodelledRoot`|A root block that is not a form's, kept as text.|
|`Diagnostics`|What the import reports, in the order of the lines it is about.|

## What is kept, reported and left out

A read does not throw on what a file holds. It keeps what the model cannot hold as text, and reports it:

|Diagnostic|Severity|What the reader does|
|---|---|---|
|`NoDesignerBlock`|Error|No form.|
|`NotAFormRoot`|Error|No form. The root block is kept as text (`UnmodelledRoot`), and the header lines are left out.|
|`NamelessComponent`|Warning (Error for the root)|The block is kept as text.|
|`NestingTooDeep`|Warning|A block nested deeper than 2,500 (`MaxNestingDepth`, the root included) is kept as text, with its contents.|
|`MalformedLine`|Warning|The line is left out.|
|`UnbalancedBlock`|Warning|A `BeginProperty` left open ends at the `End` of its block; a `Begin` left open ends with the file.|
|`UnmatchedEnd`|Warning|An `EndProperty` that closes nothing is left out.|
|`InvalidValue`|Warning|The property is kept as an `UnmodelledProperty`: a value it cannot hold, a number outside its type's range, or an `Index` that is not a number from 0 to 32767.|
|`MissingCompanion`, `MissingRecord`|Warning|The property stays at its default, and its citation is kept as an `UnmodelledProperty`.|
|`MisplacedComponent`|Warning|A component placed in one that cannot hold it is placed in the nearest enclosing container that can, at its written position.|
|`UnmodelledClass`|Information|The component is kept as an `UnmodelledComponent`, with its text.|
|`UnreadLibraryReference`, `UnmodelledHeaderLine`|Information|The header line is kept in `UnmodelledHeader`.|

A property or a property bag the model does not have is kept as an `UnmodelledProperty` and not reported. The `VERSION` line is left out. A property a block sets twice has the value of its last line.

## Positions and fonts

Positions and sizes are in the container's scale, as the file holds them: twips, unless the container (a form, a UserControl, a PropertyPage or a PictureBox) has a `ScaleMode` of its own. The root's own position and size are its client area's, in twips.

A control that writes no `Font`, on a form that writes one, takes a copy of the form's font.

## Differences from VB6

The fixtures in the tests were built and run under VB6, and the tests assert what VB6 read from them.

> [!NOTE]
> **Not implemented.** VB6 offsets a control written inside a Timer by the Timer's `Left` and `Top`. The reader places it in the form at its written position.

|Case|VB6 reads|The reader keeps|
|---|---|---|
|A user-scale Line's ends, written `0 0 100 50`|`-0.0124842 0 100.0125 50`|The written values.|
|A form's `ScaleWidth`, written `8000`|`7995`, its window's client width|The written value.|

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](vb6.fr.md) • IMPORT | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

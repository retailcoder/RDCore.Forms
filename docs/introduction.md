# Introduction
<sup>_Ce document est disponible en [français](introduction.fr.md)_</sup>

RDCore.Forms is the form library of the [RDCore](https://github.com/rubberduck-vba/RDCore) platform. This site is for the developers who use it, and for those who contribute to it.

## What it holds

|Library|What it holds|
|---|---|
|`RDCore.Forms`|The form model: the classes of components, their properties and events, the components of a form, and the reader that imports a Visual Basic 6.0 form into the model. It has no UI dependency.|
|`RDCore.Forms.Avalonia`|The controls of a form, and the window built from the model, rendered with [Avalonia](https://avaloniaui.net).|

> [!NOTE]
> **Not implemented.** `RDCore.Forms.Avalonia` holds no controls and no renderer: no form is shown on screen.

## How RDCore uses it

The RDCore language server reads a form into the model and analyses it, without loading Avalonia. The RDCore runtime runs the code behind a form; RDCore.Forms shows the form and raises its controls' events to that code.

## The model

A form is a [`FormDefinition`](api/RDCore.Forms.Model.FormDefinition.yml): a root component, the form itself, and the components placed in it. Each component is a [`ComponentInstance`](api/RDCore.Forms.Model.ComponentInstance.yml) of a [`ComponentClass`](api/RDCore.Forms.Model.ComponentClass.yml), such as `VB.CommandButton`, with the properties set on it. The classes of Visual Basic 6.0's intrinsic controls are in [`VBClasses`](api/RDCore.Forms.Model.VB.VBClasses.yml), and their properties in [`VBProperties`](api/RDCore.Forms.Model.VB.VBProperties.yml).

What a form sets that the model does not have is kept as it was written: a property as an [`UnmodelledProperty`](api/RDCore.Forms.Model.UnmodelledProperty.yml), and a component of a class the model does not have, such as an ActiveX control, as an [`UnmodelledComponent`](api/RDCore.Forms.Model.UnmodelledComponent.yml).

## Project status

RDCore.Forms is in **pre-alpha** development. No package is published: [Getting started](getting-started.md) builds it from source.

---
[ACCUEIL](index.fr.md) • [HOME](index.md) | [BIENVENUE](introduction.fr.md) • WELCOME | [DÉMARRER](getting-started.fr.md) • [GET STARTED](getting-started.md) | [IMPORTER](import/vb6.fr.md) • [IMPORT](import/vb6.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

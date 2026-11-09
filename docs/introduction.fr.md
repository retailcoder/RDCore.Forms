# Introduction
<sup>_This document is available in [English](introduction.md)_</sup>

RDCore.Forms est la bibliothèque de formulaires de la plateforme [RDCore](https://github.com/rubberduck-vba/RDCore). Ce site s'adresse aux développeurs qui l'utilisent, et à ceux qui y contribuent.

## Ce qu'elle contient

|Bibliothèque|Ce qu'elle contient|
|---|---|
|`RDCore.Forms`|Le modèle des formulaires : les classes de composants, leurs propriétés et leurs événements, les composants d'un formulaire, et le lecteur qui importe un formulaire Visual Basic 6.0 dans le modèle. Elle n'a aucune dépendance d'interface.|
|`RDCore.Forms.Avalonia`|Les contrôles d'un formulaire, et la fenêtre construite à partir du modèle, rendue avec [Avalonia](https://avaloniaui.net).|

> [!NOTE]
> **Non implémenté.** `RDCore.Forms.Avalonia` ne contient aucun contrôle ni aucun moteur de rendu : aucun formulaire n'est affiché à l'écran.

## Comment RDCore l'utilise

Le serveur de langage de RDCore lit un formulaire dans le modèle et l'analyse, sans charger Avalonia. Le _runtime_ de RDCore exécute le code d'un formulaire; RDCore.Forms affiche le formulaire et transmet à ce code les événements de ses contrôles.

## Le modèle

Un formulaire est une [`FormDefinition`](api/RDCore.Forms.Model.FormDefinition.yml) : un composant racine, le formulaire lui-même, et les composants qui y sont placés. Chaque composant est une [`ComponentInstance`](api/RDCore.Forms.Model.ComponentInstance.yml) d'une [`ComponentClass`](api/RDCore.Forms.Model.ComponentClass.yml), comme `VB.CommandButton`, avec les propriétés qui lui sont attribuées. Les classes des contrôles intrinsèques de Visual Basic 6.0 sont dans [`VBClasses`](api/RDCore.Forms.Model.VB.VBClasses.yml), et leurs propriétés dans [`VBProperties`](api/RDCore.Forms.Model.VB.VBProperties.yml).

Ce qu'un formulaire définit et que le modèle n'a pas est conservé tel qu'il a été écrit : une propriété, sous forme de [`UnmodelledProperty`](api/RDCore.Forms.Model.UnmodelledProperty.yml), et un composant d'une classe que le modèle n'a pas, comme un contrôle ActiveX, sous forme de [`UnmodelledComponent`](api/RDCore.Forms.Model.UnmodelledComponent.yml).

## État du projet

RDCore.Forms est en développement **pré-alpha**. Aucun paquet n'est publié : [Démarrer](getting-started.fr.md) le compile à partir des sources.

---
[ACCUEIL](index.fr.md) • [HOME](index.md) | BIENVENUE • [WELCOME](introduction.md) | [DÉMARRER](getting-started.fr.md) • [GET STARTED](getting-started.md) | [IMPORTER](import/vb6.fr.md) • [IMPORT](import/vb6.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

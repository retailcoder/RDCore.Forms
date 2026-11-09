# Importer des formulaires VB6
<sup>_This document is available in [English](vb6.md)_</sup>

[`VB6FormReader`](../api/RDCore.Forms.Import.VB6.VB6FormReader.yml) lit les fichiers de conception d'un projet Visual Basic 6.0 dans le modèle des formulaires : les formulaires (`.frm`), les contrôles utilisateur (`.ctl`) et les pages de propriétés (`.pag`), avec les fichiers binaires associés qu'ils citent (`.frx`, `.ctx`, `.pgx`). Il ne fait que lire : rien n'écrit de fichier de conception.

## Lire

|Méthode|Lit|
|---|---|
|`ReadFile(path, options)`|Un fichier de conception, et chaque fichier associé qu'il cite, dans le dossier du fichier de conception, le nom étant comparé sans égard à la casse.|
|`Read(text, companion, options)`|Le texte d'un fichier de conception, et les octets de son fichier associé, ou aucun.|

Le texte est décodé en Windows-1252, sauf si [`VB6ImportOptions.Encoding`](../api/RDCore.Forms.Import.VB6.VB6ImportOptions.yml) en indique un autre.

`ReadFile` ne lit un fichier associé que dans le dossier du fichier de conception. Un nom cité qui est un chemin absolu, comme un chemin réseau, n'est pas lu, ni un nom qui contient un NUL : chacun est signalé comme un fichier associé introuvable.

## Le résultat

Une lecture retourne un [`VB6FormImport`](../api/RDCore.Forms.Import.VB6.VB6FormImport.yml) :

|Membre|Contient|
|---|---|
|`Form`|Le formulaire, ou `null` quand le fichier n'en contient aucun.|
|`ModuleText`, `ModuleLine`|Le code qui suit la partie de conception (ses lignes `Attribute` comprises), et la ligne où il commence.|
|`UnmodelledHeader`|Les lignes d'en-tête qui ne désignent aucune bibliothèque de contrôles que le formulaire liste, telles qu'elles ont été lues.|
|`UnmodelledRoot`|Un bloc racine qui n'est pas celui d'un formulaire, conservé sous forme de texte.|
|`Diagnostics`|Ce que l'importation signale, dans l'ordre des lignes concernées.|

## Ce qui est conservé, signalé ou omis

Une lecture ne lève pas d'exception à cause du contenu d'un fichier. Elle conserve sous forme de texte ce que le modèle ne peut pas contenir, et le signale :

|Diagnostic|Gravité|Ce que fait le lecteur|
|---|---|---|
|`NoDesignerBlock`|Erreur|Aucun formulaire.|
|`NotAFormRoot`|Erreur|Aucun formulaire. Le bloc racine est conservé sous forme de texte (`UnmodelledRoot`), et les lignes d'en-tête sont omises.|
|`NamelessComponent`|Avertissement (Erreur pour la racine)|Le bloc est conservé sous forme de texte.|
|`NestingTooDeep`|Avertissement|Un bloc imbriqué au-delà de 2 500 niveaux (`MaxNestingDepth`, racine comprise) est conservé sous forme de texte, avec son contenu.|
|`MalformedLine`|Avertissement|La ligne est omise.|
|`UnbalancedBlock`|Avertissement|Un `BeginProperty` resté ouvert se termine au `End` de son bloc; un `Begin` resté ouvert se termine avec le fichier.|
|`UnmatchedEnd`|Avertissement|Un `EndProperty` qui ne ferme rien est omis.|
|`InvalidValue`|Avertissement|La propriété est conservée sous forme d'`UnmodelledProperty` : une valeur qu'elle ne peut pas contenir, un nombre hors de la plage de son type, ou un `Index` qui n'est pas un nombre de 0 à 32767.|
|`MissingCompanion`, `MissingRecord`|Avertissement|La propriété garde sa valeur par défaut, et sa citation est conservée sous forme d'`UnmodelledProperty`.|
|`MisplacedComponent`|Avertissement|Un composant placé dans un autre qui ne peut pas le contenir est placé dans le conteneur englobant le plus proche qui le peut, à sa position écrite.|
|`UnmodelledClass`|Information|Le composant est conservé sous forme d'`UnmodelledComponent`, avec son texte.|
|`UnreadLibraryReference`, `UnmodelledHeaderLine`|Information|La ligne d'en-tête est conservée dans `UnmodelledHeader`.|

Une propriété ou un sac de propriétés que le modèle n'a pas est conservé sous forme d'`UnmodelledProperty`, sans être signalé. La ligne `VERSION` est omise. Une propriété qu'un bloc définit deux fois prend la valeur de sa dernière ligne.

## Positions et polices

Les positions et les tailles sont dans l'échelle du conteneur, telles que le fichier les contient : en twips, sauf si le conteneur (un formulaire, un UserControl, une PropertyPage ou une PictureBox) a son propre `ScaleMode`. La position et la taille de la racine sont celles de sa zone cliente, en twips.

Un contrôle qui n'écrit aucune `Font`, sur un formulaire qui en écrit une, reçoit une copie de la police du formulaire.

## Différences avec VB6

Les fichiers de test ont été compilés et exécutés sous VB6, et les tests vérifient ce que VB6 en a lu.

> [!NOTE]
> **Non implémenté.** VB6 décale un contrôle écrit à l'intérieur d'un Timer selon le `Left` et le `Top` du Timer. Le lecteur le place dans le formulaire à sa position écrite.

|Cas|VB6 lit|Le lecteur conserve|
|---|---|---|
|Les extrémités d'une Line en échelle utilisateur, écrites `0 0 100 50`|`-0.0124842 0 100.0125 50`|Les valeurs écrites.|
|Le `ScaleWidth` d'un formulaire, écrit `8000`|`7995`, la largeur cliente de sa fenêtre|La valeur écrite.|

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | IMPORTER • [IMPORT](vb6.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

---

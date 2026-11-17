# Importer des formulaires MSForms (UserForms VBA)
<sup>_This document is available in [English](msforms.md)_</sup>

`MSFormsFormReader` lit un UserForm VBA, tel que le VBE l'exporte, dans le modèle des formulaires : le bloc de conception du fichier `.frm` et les données de conception que son `OleObjectBlob` cite dans le fichier `.frx`. RDCore.SDK lit le fichier `.frx`; le lecteur porte ce qu'il contient vers les classes, les membres et les unités de RDForms. Il ne fait que lire : rien n'écrit de UserForm.

## Lire

|Méthode|Lit|
|---|---|
|`ReadFile(path, encoding)`|Un fichier `.frm`, et le fichier `.frx` que son `OleObjectBlob` cite dans le même dossier, le nom étant comparé sans égard à la casse.|
|`Read(text, companion)`|Le texte d'un fichier `.frm`, et les octets de son fichier `.frx`, ou aucun.|
|`Read(text, design)`|Le texte d'un fichier `.frm`, et ses données de conception telles que `MSFormsReader` de RDCore.SDK les a lues.|

Le texte est décodé en Windows-1252, sauf si `ReadFile` reçoit un autre encodage. Un nom cité qui est un chemin absolu, ou qui contient un NUL, n'est pas lu.

## Le résultat

Une lecture retourne un `MSFormsFormImport` :

|Membre|Contient|
|---|---|
|`Form`|Le formulaire, un `RDForms.Form`, ou `null` quand le fichier ne contient aucun UserForm.|
|`ModuleText`, `ModuleLine`|Le code qui suit le bloc de conception (ses lignes `Attribute` comprises), et la ligne où il commence.|
|`Diagnostics`|Ce que l'importation signale : ce qui concerne le formulaire, puis ses contrôles dans l'ordre où le formulaire les enregistre.|

## Les classes

Chaque classe MSForms est lue comme la classe RDForms que lui donne la table de portage MSForms : un `TextBox` est un `RDForms.TextBox`, et un `UserForm` un `RDForms.Form`. Un `ScrollBar` est un `RDForms.HScrollBar` ou un `RDForms.VScrollBar` selon son `Orientation`; avec `fmOrientationAuto`, une barre de défilement plus large que haute est horizontale. Les contrôles d'un Frame ou d'une page y sont placés.

## Les valeurs

|MSForms|RDForms|
|---|---|
|`Left`, `Top`, `Width`, `Height`, en points|Les mêmes, en pixels (1/96 de pouce, 0,75 point). La taille du formulaire est celle de sa zone cliente, d'après les `ClientWidth` et `ClientHeight` du fichier `.frm` en twips (15 par pixel).|
|`ControlTipText`|`ToolTipText`.|
|`TextAlign`|`Alignment`, avec les valeurs de VB : gauche 0, droite 1, centre 2.|
|Le `Value` d'un TextBox, le `Value` d'un ComboBox|`Text`.|
|Un ToggleButton|Un `RDForms.ToggleButton`. Son `Value` vaut True quand le formulaire enregistre 1, et False quand il enregistre 0 ou rien.|
|Un SpinButton|Un `RDForms.SpinButton`, avec ses `Value`, `Min`, `Max`, `SmallChange`, `Orientation` et `Delay`.|
|Un MultiPage|Un `RDForms.MultiPage`. Chacune de ses pages est un `RDForms.Page`, avec le `Caption` de son onglet et les contrôles qu'elle contient; une page remplit son MultiPage, donc sa propre taille n'est pas gardée.|
|Un TabStrip|Un `RDForms.TabStrip`, avec ses `Tabs` dans leur ordre : le nom de chaque onglet, son `Caption` avec son `Accelerator`, son `ControlTipText` comme `ToolTipText`, son `Tag`, son `Enabled` et son `Visible`.|
|`Caption` et `Accelerator`|`Caption`, chaque `&` littéral étant doublé et un `&` étant placé devant la première lettre qui correspond à `Accelerator`. La légende d'un formulaire est gardée telle quelle.|
|`SpecialEffect`|Plat et enfoncé sont les valeurs Flat et 3D d'`Appearance`. Un Frame est dessiné gravé.|
|Le `PictureSizeMode` d'une Image|Rogner et étirer sont `Stretch` à False et à True.|
|`Picture`, `MouseIcon`|Les octets de l'image. Un CommandButton qui a une image est graphique (`Style` 1), puisque MSForms montre toujours son image.|
|`BackColor`, `ForeColor`, `Font`, `MousePointer`, `Enabled`, `Visible`, `TabIndex`, `TabStop`, `Tag`, `Default`, `Cancel` et les membres du même nom|Copiés.|
|`ControlSource`, `RowSource`|Gardés sous forme de texte, dans les mêmes membres. Rien ne les résout, et chacun est signalé.|

Une valeur n'est affectée que si elle diffère de la valeur par défaut de la classe RDForms.

> [!NOTE]
> **Non implémenté.** Une image en métafichier est gardée mais pas dessinée. `Zoom` et `IMEMode` ne sont pas implémentés : un formulaire ou un contrôle qui en affecte un autre que sa valeur par défaut le garde sous forme de texte, avec un diagnostic.

## Ce qui est gardé, signalé et laissé de côté

Une lecture ne lève pas d'exception pour ce que contient un fichier. Une propriété que le modèle n'a pas est gardée comme `UnmodelledProperty` et n'est pas signalée, comme le fait le lecteur VB6.

|Diagnostic|Gravité|Ce que fait le lecteur|
|---|---|---|
|`NoDesignerBlock`, `NotAUserForm`|Error|Aucun formulaire.|
|`MalformedLine`|Warning|La ligne est laissée de côté.|
|`MissingCompanion`, `UnreadableDesignerData`|Warning|Le formulaire est lu à partir de son seul bloc de conception, sans contrôles.|
|`UnmodelledClass`|Information|Le contrôle est gardé comme `UnmodelledComponent`, sans texte : ses données de conception sont binaires. Les contrôles qu'il contient sont laissés de côté.|
|`InvalidValue`|Warning|Une valeur que la propriété RDForms ne peut pas contenir, comme une couleur hors des couleurs OLE, est gardée sous forme de texte.|
|`ValueNotPorted`|Warning|Une valeur sans équivalent RDForms, comme un `SpecialEffect` en relief, un `PictureSizeMode` en zoom, un `GroupName`, ou l'image d'un CheckBox ou d'un OptionButton, est gardée sous forme de texte.|
|`NotImplemented`|Warning|Un `Zoom` ou un `IMEMode` autre que sa valeur par défaut est gardé sous forme de texte.|
|`MetafilePicture`|Warning|L'image est gardée mais pas dessinée.|
|`AcceleratorNotInCaption`|Warning|La légende n'a pas de touche d'accès.|
|`UnresolvedDataSource`|Warning|Le `ControlSource` ou le `RowSource` est gardé sous forme de texte. Son `Port` est le diagnostic de portage RDC40007.|

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](vb6.fr.md) • [IMPORT](vb6.md) | [CONTROLS](../controls/index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)

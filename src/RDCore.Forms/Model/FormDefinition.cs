// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Globalization;
using RDCore.Forms.Drawing;

namespace RDCore.Forms.Model;

/// <summary>
/// A form's designer: its root component, with every control and menu item placed in it, and what the form declares about itself.
/// </summary>
/// <remarks>
/// The form's code is not part of its definition. A control's position and size are in pixels (device-independent pixels, 1/96
/// inch), whatever its container's <c>ScaleMode</c>; a program's code reads them in its own unit (see <see cref="CodeUnits"/>). A VB
/// Line's ends are in its container's scale: twips, unless its container (a form, a UserControl, a PropertyPage or a PictureBox) has a
/// <c>ScaleMode</c> of its own. The root's own position and size are its client area's, in pixels.
/// </remarks>
public sealed class FormDefinition
{
    // The controls added at run time and not removed, in the order they were added.
    private readonly List<ComponentInstance> added = [];

    // The control arrays asked for, by name, so that each is one object.
    private readonly Dictionary<string, FormControlArray> arrays = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Creates the definition of a form whose root is <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The form's root component: a form, a UserControl or a PropertyPage.</param>
    /// <exception cref="ArgumentException"><paramref name="root"/> is not of a root class, or is placed in a container.</exception>
    public FormDefinition(ComponentInstance root)
    {
        if (root.Class.Kind != ComponentKind.Root)
            throw new ArgumentException($"{root.Class.TypeName} is not the class of a form's root.", nameof(root));
        if (root.Container is not null)
            throw new ArgumentException($"{root.Name} is placed in {root.Container.Name}.", nameof(root));
        Root = root;
    }

    /// <summary>
    /// The form's root component.
    /// </summary>
    public ComponentInstance Root { get; }

    /// <summary>
    /// The form's name: its root's.
    /// </summary>
    public string Name => Root.Name;

    /// <summary>
    /// The control libraries the form's file declares, in its order: the ActiveX controls its components may be instances of.
    /// </summary>
    public List<ControlLibraryReference> ControlLibraries { get; } = [];

    /// <summary>
    /// The form's root, then every component placed in it, depth first in the order the form's file holds them.
    /// </summary>
    public IEnumerable<ComponentInstance> Components => Root.Descendants().Prepend(Root);

    /// <summary>
    /// The form's component of a given name and control-array index, the name compared without regard to case.
    /// </summary>
    /// <param name="name">The component's name.</param>
    /// <param name="index">The component's index in its control array, or <see langword="null"/> for a component not in one.</param>
    /// <returns>The component, or <see langword="null"/> when the form has none of that name and index.</returns>
    public ComponentInstance? Find(string name, int? index = null) =>
        Components.FirstOrDefault(c => c.Index == index && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The form's control array of a name, the name compared without regard to case: the controls of that name that have an index.
    /// Menu items are in menu arrays (<see cref="Menus.FormMenu.ArrayOf"/>).
    /// </summary>
    /// <param name="name">The array's name.</param>
    /// <returns>The array, the same object each time; <see langword="null"/> when no control of that name has an index.</returns>
    public FormControlArray? ArrayOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!Root.Descendants().Any(component => FormControlArray.IsElement(component, name)))
            return null;
        if (!arrays.TryGetValue(name, out var array))
            arrays.Add(name, array = new FormControlArray(this, name));
        return array;
    }

    /// <summary>
    /// Raised after <see cref="AddControl(ComponentClass, string, ComponentInstance?)"/> places a control in the form, and after
    /// <see cref="FormControlArray.Load"/> loads an element of a control array.
    /// </summary>
    public event EventHandler<FormControlEventArgs>? ControlAdded;

    /// <summary>
    /// Raised after <see cref="RemoveControl(ComponentInstance)"/> takes a control, and what it holds, out of the form.
    /// </summary>
    public event EventHandler<FormControlEventArgs>? ControlRemoved;

    /// <summary>
    /// The form's controls and menu items as a program's <c>Controls</c> collection holds them: those of the form's file, depth first
    /// in its order, then those added at run time, in the order they were added.
    /// </summary>
    public IReadOnlyList<ComponentInstance> Controls =>
        [.. Root.Descendants().Where(c => !added.Contains(c)), .. added];

    /// <summary>
    /// Whether a component was added by <see cref="AddControl(ComponentClass, string, ComponentInstance?)"/> and not removed since.
    /// </summary>
    /// <param name="component">The component.</param>
    /// <returns><see langword="true"/> for a control added at run time.</returns>
    public bool IsAddedAtRunTime(ComponentInstance component) => added.Contains(component);

    /// <summary>
    /// Makes a copy of the form, which has components of its own: each one a copy of one of this form's, with its class, name, index,
    /// the values set on it and what it keeps as text, in the same container at the same place. A copy of a control added at run
    /// time is one the copy added at run time. The copy declares the same control libraries.
    /// </summary>
    /// <returns>The copy. A change to it, or to one of its components, leaves this form as it is, and the other way round.</returns>
    /// <remarks>
    /// Each UserControl a form places has a copy of its UserControl's definition, so that its controls are its own.
    /// </remarks>
    public FormDefinition Copy()
    {
        var copies = new Dictionary<ComponentInstance, ComponentInstance>(ReferenceEqualityComparer.Instance);
        foreach (var component in Components)
        {
            var copy = new ComponentInstance(component.Class, component.Name, component.Index);
            foreach (var (property, value) in component.SetValues)
                copy.TryRead(property, value);
            copy.UnmodelledProperties.AddRange(component.UnmodelledProperties);
            copy.UnmodelledContents.AddRange(component.UnmodelledContents);
            if (component.Container is { } container)
                copy.SetContainer(copies[container]);
            copies.Add(component, copy);
        }
        var form = new FormDefinition(copies[Root]);
        form.ControlLibraries.AddRange(ControlLibraries);
        form.added.AddRange(added.Select(control => copies[control]));
        return form;
    }

    /// <summary>
    /// Adds a control of an RDForms class to the form, as a program's <c>Controls.Add</c> does: the class named by its ProgID, such
    /// as <c>RDForms.CommandButton</c>, without regard to case.
    /// </summary>
    /// <param name="progId">The class's ProgID.</param>
    /// <param name="name">The control's name.</param>
    /// <param name="container">The component to place it in: the root, or a container control of the form. <see langword="null"/>
    /// places it in the root.</param>
    /// <returns>The control added.</returns>
    /// <exception cref="FormErrorException">
    /// No RDForms class has that ProgID: 711, Invalid class string. Otherwise as
    /// <see cref="AddControl(ComponentClass, string, ComponentInstance?)"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="container"/> is not a component of this form.</exception>
    public ComponentInstance AddControl(string progId, string name, ComponentInstance? container = null)
    {
        ArgumentNullException.ThrowIfNull(progId);
        var type = ComponentClasses.All.FirstOrDefault(c => string.Equals(c.TypeName, progId, StringComparison.OrdinalIgnoreCase))
                   ?? throw new FormErrorException(FormError.InvalidClassString(progId));
        return AddControl(type, name, container);
    }

    /// <summary>
    /// Adds a control to the form, as a program's <c>Controls.Add</c> does, and raises <see cref="ControlAdded"/>.
    /// </summary>
    /// <param name="type">The control's class.</param>
    /// <param name="name">The control's name.</param>
    /// <param name="container">The component to place it in: the root, or a container control of the form. <see langword="null"/>
    /// places it in the root.</param>
    /// <returns>The control added.</returns>
    /// <exception cref="FormErrorException">
    /// <paramref name="type"/> is a menu's or a root's class: 720, not a valid control type. <paramref name="name"/> is empty or
    /// white space: 728, not a legal control name. A component of the form other than its root has the name, compared without regard
    /// to case: 727. <paramref name="container"/> holds no controls, or none of the class: 721, not a valid control container. A page goes in
    /// a MultiPage only, and a MultiPage holds pages only (<see cref="ComponentClasses.CanHold"/>).
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="container"/> is not a component of this form.</exception>
    /// <remarks>
    /// <para>
    /// The control is placed last in its container, so that it is under the controls already there. It is hidden, at 0, 0, and 80
    /// by 32 pixels (1200 by 480 twips) where its class has an extent. Its <c>TabIndex</c>, where its class has one, is the next
    /// after the form's others. Its other properties have their class's defaults, so that a caption or a text is "".
    /// </para>
    /// <para>
    /// A page is shown, as a MultiPage shows its selected page. A MultiPage starts with two pages, captioned as they are named:
    /// <c>Page1</c> and <c>Page2</c>, or the next names no component of the form has.
    /// </para>
    /// <para>
    /// Any other name is accepted, even one that starts with a digit, holds a space, is a keyword, or is the form's own name.
    /// </para>
    /// </remarks>
    public ComponentInstance AddControl(ComponentClass type, string name, ComponentInstance? container = null) =>
        AddControl(type, name, container, -1);

    // AddControl, at index among the container's contents (last where it is negative). A page is shown, and a MultiPage starts with two
    // of them, Page1 and Page2 or the next names the form has free, as MSForms gives one; they are among the controls added.
    internal ComponentInstance AddControl(ComponentClass type, string name, ComponentInstance? container, int index)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(name);
        container ??= Root;
        if (!ReferenceEquals(container, Root) && !Root.Descendants().Contains(container))
            throw new ArgumentException($"{container.Name} is not a component of {Name}.", nameof(container));
        if (type.Kind != ComponentKind.Control)
            throw new FormErrorException(FormError.NotAValidControlType(type.ClassName));
        if (string.IsNullOrWhiteSpace(name))
            throw new FormErrorException(FormError.NotALegalControlName(name));
        if (IsNameTaken(name))
            throw new FormErrorException(FormError.ControlNameTaken(name));
        if (!ComponentClasses.CanHold(container.Class, type))
            throw new FormErrorException(FormError.NotAValidControlContainer(container.Class.ClassName));
        var control = New(type, name);
        control.SetContainer(container, index);
        added.Add(control);
        if (ReferenceEquals(type, ComponentClasses.MultiPage))
        {
            foreach (var _ in Enumerable.Range(0, 2))
            {
                var page = New(ComponentClasses.Page, FreeName(ComponentClasses.Page.DefaultName));
                page.Set(Properties.Caption, page.Name).SetContainer(control);
                added.Add(page);
            }
        }

        ControlAdded?.Invoke(this, new FormControlEventArgs(control, container));
        return control;
    }

    // Whether a component of the form other than its root has the name, compared without regard to case.
    internal bool IsNameTaken(string name) => Root.Descendants().Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    // The first of stem1, stem2, ... that no component of the form has.
    internal string FreeName(string stem)
    {
        var number = 1;
        while (IsNameTaken(stem + number.ToString(CultureInfo.InvariantCulture)))
            number++;
        return stem + number.ToString(CultureInfo.InvariantCulture);
    }

    private ComponentInstance New(ComponentClass type, string name)
    {
        var control = new ComponentInstance(type, name);
        if (type.Has(Properties.Visible) && !ReferenceEquals(type, ComponentClasses.Page))
            control.Set(Properties.Visible, false);
        if (type.HasExtent)
            control.Set(Properties.Width, 80d).Set(Properties.Height, 32d);
        if (type.Has(Properties.TabIndex))
            control.Set(Properties.TabIndex, (short)Root.Descendants().Count(c => c.Class.Has(Properties.TabIndex)));
        return control;
    }

    // A new element of a control array, of an index, copied from another (FormControlArray.Load): hidden, with the next TabIndex, last in
    // the other's container, and added at run time, so that Unload can take it out.
    internal ComponentInstance LoadElement(ComponentInstance source, int index)
    {
        var container = source.Container!;
        var element = new ComponentInstance(source.Class, source.Name, index);
        foreach (var (property, value) in source.SetValues)
        {
            if (property != Properties.Visible && property != Properties.TabIndex)
                element.TryRead(property, value);
        }

        if (element.Class.Has(Properties.Visible))
            element.Set(Properties.Visible, false);
        if (element.Class.Has(Properties.TabIndex))
            element.Set(Properties.TabIndex, (short)Root.Descendants().Count(c => c.Class.Has(Properties.TabIndex)));
        element.SetContainer(container);
        added.Add(element);
        ControlAdded?.Invoke(this, new FormControlEventArgs(element, container));
        return element;
    }

    /// <summary>
    /// Renames a control, as a program's assignment of its <c>Name</c> does on an RDForms form. Microsoft Forms 2.0 lets a program rename a control it
    /// added (<see cref="AddControl(ComponentClass, string, ComponentInstance?)"/>), and no other.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <param name="name">Its new name. Any name, as Microsoft Forms 2.0 takes it: one taken, one that is not a legal control name, or none.</param>
    /// <exception cref="FormErrorException">
    /// The control is not one of the form's: 730, Control not found. It is one of the form's file, or an element of a control array: 382, "Could not set the
    /// Name property. Can not set property at runtime.", as Microsoft Forms 2.0 says it. Its name stays.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="control"/> or <paramref name="name"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The control is then found by its new name, not by its old one.
    /// </remarks>
    public void RenameControl(ComponentInstance control, string name)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(name);
        if (!Root.Descendants().Contains(control))
            throw new FormErrorException(FormError.ControlNotFound(control.Name));
        if (!added.Contains(control) || control.Index is not null)
            throw new FormErrorException(FormError.PropertyNotSetAtRuntime("Name"));
        control.Rename(name);
    }

    /// <summary>
    /// Removes the control that has a name, as a program's <c>Controls.Remove</c> does with a name.
    /// </summary>
    /// <param name="name">The control's name, compared without regard to case.</param>
    /// <exception cref="FormErrorException">
    /// The form has no control of that name: 730, Control not found. The name is a control array's: -2147467262 (&amp;H80004002), No such
    /// interface supported. Otherwise as <see cref="RemoveControl(ComponentInstance)"/>.
    /// </exception>
    public void RemoveControl(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        // VB6 takes the name for the array object, which lacks the interface of a control.
        if (ArrayOf(name) is not null)
            throw new FormErrorException(FormError.NoSuchInterface);
        var control = Root.Descendants().FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
                      ?? throw new FormErrorException(FormError.ControlNotFound(name));
        RemoveControl(control);
    }

    /// <summary>
    /// Takes a control added at run time out of the form, with the controls it holds, as a program's <c>Controls.Remove</c> does,
    /// and raises <see cref="ControlRemoved"/>.
    /// </summary>
    /// <param name="control">The control.</param>
    /// <exception cref="FormErrorException">
    /// The control is not one of the form's: 730, Control not found. It was not added at run time, or is an element of a control array,
    /// even one a program's <c>Load</c> added: 729. It holds a control that was not added at run time: 740. Nothing is removed.
    /// </exception>
    /// <remarks>
    /// The form's other <c>TabIndex</c> values close up over those taken out, so that they run on from 0 with no gap.
    /// </remarks>
    public void RemoveControl(ComponentInstance control)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (!Root.Descendants().Contains(control))
            throw new FormErrorException(FormError.ControlNotFound(control.Name));
        // an element a program loaded is the Unload statement's to take out (FormControlArray.Unload).
        if (!added.Contains(control) || control.Index is not null)
            throw new FormErrorException(FormError.CanOnlyRemoveAddedControls);
        RemoveAdded(control);
    }

    // An element of a control array that a program loaded, which Unload takes out as Controls.Remove takes out a control it added.
    internal void UnloadElement(ComponentInstance element) => RemoveAdded(element);

    private void RemoveAdded(ComponentInstance control)
    {
        var removed = control.Descendants().Prepend(control).ToList();
        if (removed.FirstOrDefault(c => !added.Contains(c)) is { } placed)
            throw new FormErrorException(FormError.ContainerHoldsADesignTimeControl(placed.Name));
        var container = control.Container!;
        control.SetContainer(null);
        foreach (var component in removed)
            added.Remove(component);
        var freed = removed.Where(c => c.Class.Has(Properties.TabIndex)).Select(c => c.Get(Properties.TabIndex)).ToList();
        foreach (var other in Root.Descendants().Where(c => c.Class.Has(Properties.TabIndex)))
        {
            var tabIndex = other.Get(Properties.TabIndex);
            var below = freed.Count(f => f < tabIndex);
            if (below > 0)
                other.Set(Properties.TabIndex, (short)(tabIndex - below));
        }
        ControlRemoved?.Invoke(this, new FormControlEventArgs(control, container));
    }

    // Takes a page out of its MultiPage, with the controls on it, as Pages.Remove does: unlike Controls.Remove, a page placed at design
    // time goes too, and the controls on it with it.
    internal void RemovePage(ComponentInstance page)
    {
        if (!ReferenceEquals(page.Class, ComponentClasses.Page) || !Root.Descendants().Contains(page))
            throw new FormErrorException(FormError.ControlNotFound(page.Name));
        foreach (var component in page.Descendants().Prepend(page).Where(c => !added.Contains(c)))
            added.Add(component);
        RemoveControl(page);
    }

    /// <summary>
    /// Raised after <see cref="SetContainer"/> places a control, with what it holds, in a container.
    /// </summary>
    public event EventHandler<FormContainerChangedEventArgs>? ContainerChanged;

    /// <summary>
    /// Places a control in another container of the form, with the controls it holds, as a program's <c>Set Control.Container</c>
    /// does, and raises <see cref="ContainerChanged"/>.
    /// </summary>
    /// <param name="control">The control: one of the form's, whose class <see cref="ComponentClass.HasContainer"/>.</param>
    /// <param name="container">The container: the root, or a control of the form that holds controls.</param>
    /// <exception cref="FormErrorException">
    /// 425, Invalid object use, and nothing changes: <paramref name="container"/> is <see langword="null"/>, is not the root or a
    /// control of this form, holds no controls or none of the control's class (<see cref="ComponentClasses.CanHold"/>: a page goes in a
    /// MultiPage only, and a MultiPage holds pages only), or is <paramref name="control"/> or a control it holds.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="control"/> is not a control of this form, or its class has no <c>Container</c>.
    /// </exception>
    /// <remarks>
    /// The control keeps its place in pixels in its container's client area, so a program reads its <c>Left</c> and <c>Top</c> in the
    /// new container's scale, and the ends of a Line are given again in that scale. It is placed last in the new container's
    /// <see cref="ComponentInstance.Contents"/>, even where that is the container it was in. Its <c>Visible</c> and every
    /// <c>TabIndex</c> are as they were, and a control added at run time can still be removed.
    /// </remarks>
    public void SetContainer(ComponentInstance control, ComponentInstance? container)
    {
        ArgumentNullException.ThrowIfNull(control);
        if (!Root.Descendants().Contains(control))
            throw new ArgumentException($"{control.Name} is not a control of {Name}.", nameof(control));
        if (!control.Class.HasContainer)
            throw new ArgumentException($"{control.Class.ClassName} has no Container.", nameof(control));
        if (container is null || !ComponentClasses.CanHold(container.Class, control.Class)
            || (!ReferenceEquals(container, Root) && !Root.Descendants().Contains(container))
            || ReferenceEquals(container, control) || control.Descendants().Contains(container))
            throw new FormErrorException(FormError.InvalidObjectUse);
        var previous = control.Container!;
        (double X1, double Y1, double X2, double Y2)? line = ReferenceEquals(control.Class, ComponentClasses.Line) && !ReferenceEquals(previous, container)
            ? LineEnds(control, DrawingScale.Of(previous), DrawingScale.Of(container))
            : null;
        control.SetContainer(container);
        if (line is { } ends)
            control.Set(Properties.X1, ends.X1).Set(Properties.Y1, ends.Y1).Set(Properties.X2, ends.X2).Set(Properties.Y2, ends.Y2);
        ContainerChanged?.Invoke(this, new FormContainerChangedEventArgs(control, previous, container));
    }

    // A Line's ends at the same place, given in the new container's scale.
    private static (double X1, double Y1, double X2, double Y2) LineEnds(ComponentInstance line, DrawingScale from, DrawingScale to)
    {
        var (x1, y1) = from.ToTwips(line.Get(Properties.X1), line.Get(Properties.Y1));
        var (x2, y2) = from.ToTwips(line.Get(Properties.X2), line.Get(Properties.Y2));
        (x1, y1) = to.FromTwips(x1, y1);
        (x2, y2) = to.FromTwips(x2, y2);
        return (x1, y1, x2, y2);
    }
}

/// <summary>
/// The data of <see cref="FormDefinition.ContainerChanged"/>.
/// </summary>
/// <param name="control">The control placed.</param>
/// <param name="previous">The container it was in.</param>
/// <param name="container">The container it is placed in, which may be <paramref name="previous"/>.</param>
public sealed class FormContainerChangedEventArgs(ComponentInstance control, ComponentInstance previous, ComponentInstance container) : EventArgs
{
    /// <summary>
    /// The control placed, which keeps the controls it holds.
    /// </summary>
    public ComponentInstance Control { get; } = control;

    /// <summary>
    /// The container the control was in.
    /// </summary>
    public ComponentInstance Previous { get; } = previous;

    /// <summary>
    /// The container the control is placed in.
    /// </summary>
    public ComponentInstance Container { get; } = container;
}

/// <summary>
/// The data of <see cref="FormDefinition.ControlAdded"/> and <see cref="FormDefinition.ControlRemoved"/>.
/// </summary>
/// <param name="control">The control added or removed.</param>
/// <param name="container">The component it was placed in, or taken out of.</param>
public sealed class FormControlEventArgs(ComponentInstance control, ComponentInstance container) : EventArgs
{
    /// <summary>
    /// The control added or removed. A control removed keeps the controls it held.
    /// </summary>
    public ComponentInstance Control { get; } = control;

    /// <summary>
    /// The component the control was placed in, or taken out of.
    /// </summary>
    public ComponentInstance Container { get; } = container;
}

/// <summary>
/// A control library a form declares, such as <c>MSCOMCTL.OCX</c>.
/// </summary>
/// <param name="Id">The GUID of the library's type library.</param>
/// <param name="Version">The library's version, such as <c>2.0</c>.</param>
/// <param name="FileName">The library's file name, as the form's file names it.</param>
public sealed record ControlLibraryReference(Guid Id, string Version, string FileName);

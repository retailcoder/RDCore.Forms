// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model;

/// <summary>
/// A form's designer: its root component, with every control and menu item placed in it, and what the form declares about itself.
/// </summary>
/// <remarks>
/// The form's code is not part of its definition. A control's position and size, and a VB Line's ends, are in its container's scale:
/// twips, unless its container (a form, a UserControl, a PropertyPage or a PictureBox) has a <c>ScaleMode</c> of its own. The root's
/// own position and size are its client area's, in twips.
/// </remarks>
public sealed class FormDefinition
{
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
}

/// <summary>
/// A control library a form declares, such as <c>MSCOMCTL.OCX</c>.
/// </summary>
/// <param name="Id">The GUID of the library's type library.</param>
/// <param name="Version">The library's version, such as <c>2.0</c>.</param>
/// <param name="FileName">The library's file name, as the form's file names it.</param>
public sealed record ControlLibraryReference(Guid Id, string Version, string FileName);

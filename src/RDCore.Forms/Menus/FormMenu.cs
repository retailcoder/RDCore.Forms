using System.Runtime.CompilerServices;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Menus;

/// <summary>
/// A form's menus: the <c>VB.Menu</c> components its root holds, as the items of its menu bar, each holding the items of its own
/// menu.
/// </summary>
/// <remarks>
/// The items are the components themselves, read as the root holds them each time they are asked for. A component's values are its
/// <see cref="FormMenuItem"/>'s, so a change to one shows in the other.
/// </remarks>
public sealed class FormMenu
{
    private static readonly ConditionalWeakTable<ComponentInstance, FormMenu> menus = new();

    private FormMenu(ComponentInstance root) => Root = root;

    /// <summary>
    /// Gets the menus of a form.
    /// </summary>
    /// <param name="root">The form's root.</param>
    /// <returns>The form's menus: the same object each time for the same root.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="root"/> is not a designer root.</exception>
    public static FormMenu Of(ComponentInstance root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (root.Class.Kind != ComponentKind.Root)
            throw new ArgumentException($"A {root.Class.TypeName} is not a designer root.", nameof(root));
        return menus.GetValue(root, r => new FormMenu(r));
    }

    /// <summary>
    /// The form's root.
    /// </summary>
    public ComponentInstance Root { get; }

    /// <summary>
    /// The items of the menu bar: the root's menus, in the order it holds them.
    /// </summary>
    public IReadOnlyList<FormMenuItem> Items => FormMenuItem.MenusIn(Root);

    /// <summary>
    /// Whether the form shows a menu bar: one of its items is visible. A form whose menus are all hidden, as one that keeps them only
    /// to pop up, shows none.
    /// </summary>
    public bool ShowsTheBar => Items.Any(item => item.Visible);

    /// <summary>
    /// Every item of the form, each menu before its own items.
    /// </summary>
    public IEnumerable<FormMenuItem> AllItems => Items.SelectMany(item => item.WithAllItems());
}

/// <summary>
/// A menu item of a form: a <c>VB.Menu</c> component, with VB's members and the rules VB6 has for them.
/// </summary>
/// <remarks>
/// <para>
/// An item that holds items is a menu: choosing it opens its menu. An item whose <see cref="Caption"/> is a hyphen is a
/// separator, drawn as a line.
/// </para>
/// <para>
/// The setters refuse what VB6 refuses, as measured with VB6: <see cref="Checked"/> on a top-level item or a
/// separator, a separator's <see cref="Enabled"/> set to False, the last visible item of a menu hidden, and a hyphen as a caption.
/// </para>
/// </remarks>
public sealed class FormMenuItem
{
    private static readonly ConditionalWeakTable<ComponentInstance, FormMenuItem> items = new();

    private FormMenuItem(ComponentInstance component) => Component = component;

    /// <summary>
    /// Gets the item a menu component is.
    /// </summary>
    /// <param name="menu">The menu component.</param>
    /// <returns>The item: the same object each time for the same component.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="menu"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="menu"/> is not a <see cref="VBClasses.Menu"/>.</exception>
    public static FormMenuItem Of(ComponentInstance menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        if (!IsMenu(menu))
            throw new ArgumentException($"The component is a {menu.Class.TypeName}, not a VB.Menu.", nameof(menu));
        return items.GetValue(menu, m => new FormMenuItem(m));
    }

    /// <summary>
    /// The menu component.
    /// </summary>
    public ComponentInstance Component { get; }

    /// <summary>
    /// The menu that holds the item, or <see langword="null"/> for an item of the menu bar.
    /// </summary>
    public FormMenuItem? Parent => Component.Container is { } container && IsMenu(container) ? Of(container) : null;

    /// <summary>
    /// The items of the item's menu, in the order it holds them: none for an item that opens no menu.
    /// </summary>
    public IReadOnlyList<FormMenuItem> Items => MenusIn(Component);

    /// <summary>
    /// Whether the item is on the menu bar.
    /// </summary>
    public bool IsTopLevel => Parent is null;

    /// <summary>
    /// Whether the item is a separator: its <see cref="Caption"/> is a hyphen.
    /// </summary>
    public bool IsSeparator => Caption == "-";

    /// <summary>
    /// The item's text. An ampersand marks the character after it as its access key, and two ampersands show one.
    /// </summary>
    /// <exception cref="FormErrorException">
    /// Set to a hyphen on an item that is not a separator: 395, Cannot use separator bar as menu name for this control.
    /// </exception>
    public string Caption
    {
        get => Component.Get(P.Caption);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value == "-" && !IsSeparator)
                throw new FormErrorException(FormError.CantUseSeparatorBarAsMenuName);
            Component.Set(P.Caption, value);
        }
    }

    /// <summary>
    /// Whether the item shows a check mark.
    /// </summary>
    /// <exception cref="FormErrorException">
    /// Set on a top-level item or a separator: 387, 'Checked' property can't be set on this control.
    /// </exception>
    public bool Checked
    {
        get => Component.Get(P.Checked);
        set
        {
            if (IsTopLevel || IsSeparator)
                throw new FormErrorException(FormError.PropertyCantBeSetOnThisControl(P.Checked.Name));
            Component.Set(P.Checked, value);
        }
    }

    /// <summary>
    /// Whether the item can be chosen, and its menu opened. A disabled item is drawn greyed.
    /// </summary>
    /// <exception cref="FormErrorException">
    /// Set to False on a separator: 387, 'Enabled' property can't be set on this control.
    /// </exception>
    public bool Enabled
    {
        get => Component.Get(P.Enabled);
        set
        {
            if (!value && IsSeparator)
                throw new FormErrorException(FormError.PropertyCantBeSetOnThisControl(P.Enabled.Name));
            Component.Set(P.Enabled, value);
        }
    }

    /// <summary>
    /// Whether the item is shown. A hidden item takes no space and no shortcut, and the items of its menu are hidden with it.
    /// </summary>
    /// <exception cref="FormErrorException">
    /// Set to False on the last visible item of a menu, a separator included: 387, 'Visible' property can't be set on this control.
    /// The last visible item of the menu bar can be hidden, and the bar goes with it.
    /// </exception>
    public bool Visible
    {
        get => Component.Get(P.Visible);
        set
        {
            if (!value && Visible && Parent is { } menu && menu.Items.Count(item => item.Visible) == 1)
                throw new FormErrorException(FormError.PropertyCantBeSetOnThisControl(P.Visible.Name));
            Component.Set(P.Visible, value);
        }
    }

    /// <summary>
    /// The key that chooses the item from anywhere on its form, or <see langword="null"/> for none.
    /// </summary>
    public MenuShortcut? Shortcut
    {
        get => Component.Get(P.Shortcut);
        set => Component.Set(P.Shortcut, value);
    }

    /// <summary>
    /// Whether choosing the item ticks or unticks it before its Click: an RD-VBA addition. False, the default, leaves
    /// <see cref="Checked"/> to the program, as a VB6 menu does.
    /// </summary>
    public bool CheckOnClick
    {
        get => Component.Get(P.CheckOnClick);
        set => Component.Set(P.CheckOnClick, value);
    }

    /// <summary>
    /// The picture shown beside the item's caption, as the bytes of its image file, or <see langword="null"/> for none: an RD-VBA
    /// addition.
    /// </summary>
    public byte[]? Picture
    {
        get => Component.Get(P.Picture);
        set => Component.Set(P.Picture, value);
    }

    internal static IReadOnlyList<FormMenuItem> MenusIn(ComponentInstance container) =>
        [.. container.Contents.Where(IsMenu).Select(Of)];

    internal IEnumerable<FormMenuItem> WithAllItems() => [this, .. Items.SelectMany(item => item.WithAllItems())];

    private static bool IsMenu(ComponentInstance component) => ReferenceEquals(component.Class, VBClasses.Menu);
}

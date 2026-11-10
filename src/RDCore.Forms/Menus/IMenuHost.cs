using System.Runtime.CompilerServices;
using RDCore.Forms.Model;

namespace RDCore.Forms.Menus;

/// <summary>
/// Where a pop-up menu goes, across from the point it is shown at.
/// </summary>
public enum PopupMenuAlignment
{
    /// <summary>
    /// Its left edge is at the point: VB's <c>vbPopupMenuLeftAlign</c>, the default.
    /// </summary>
    Left,

    /// <summary>
    /// It is centred on the point: VB's <c>vbPopupMenuCenterAlign</c>.
    /// </summary>
    Center,

    /// <summary>
    /// Its right edge is at the point: VB's <c>vbPopupMenuRightAlign</c>.
    /// </summary>
    Right,
}

/// <summary>
/// A shown form's menus: what pops one of them up, the destination of VB's <c>PopupMenu</c> method.
/// </summary>
public interface IMenuHost
{
    /// <summary>
    /// The form's root.
    /// </summary>
    ComponentInstance Root { get; }

    /// <summary>
    /// Pops up the items of one of the form's menus, as VB's <c>PopupMenu</c> method does. The menu need not be visible on the bar: a
    /// hidden top-level menu is the usual one. It raises its Click as it opens, as it does on the bar.
    /// </summary>
    /// <param name="menu">The menu whose items pop up.</param>
    /// <param name="x">The point's horizontal coordinate in the root's scale, or <see langword="null"/> for the pointer's.</param>
    /// <param name="y">The point's vertical coordinate in the root's scale, or <see langword="null"/> for the pointer's.</param>
    /// <param name="alignment">Where the menu goes across from the point.</param>
    /// <param name="boldItem">An item of the menu to show in bold, as VB's <c>BoldCommand</c> argument, or <see langword="null"/>.</param>
    /// <returns>
    /// A task that completes when the menu closes, after the Click of an item chosen from it. A menu that holds no items shows nothing,
    /// and the task returned has already completed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="menu"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="menu"/> is not one of the form's menus.</exception>
    Task PopupMenu(FormMenuItem menu, double? x = null, double? y = null, PopupMenuAlignment alignment = PopupMenuAlignment.Left,
        FormMenuItem? boldItem = null);
}

/// <summary>
/// The menu host of each shown form, by its root: where a program's <c>PopupMenu</c> goes.
/// </summary>
/// <remarks>
/// A form's window registers itself while it is open, and unregisters when it closes. A root holds no reference to its host, so a root no
/// longer shown can be collected.
/// </remarks>
public static class MenuHosts
{
    private static readonly ConditionalWeakTable<ComponentInstance, IMenuHost> hosts = new();

    /// <summary>
    /// The host of a form's menus.
    /// </summary>
    /// <param name="root">The form's root.</param>
    /// <returns>Its host, or <see langword="null"/> where the form is not shown.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    public static IMenuHost? Of(ComponentInstance root)
    {
        ArgumentNullException.ThrowIfNull(root);
        return hosts.TryGetValue(root, out var host) ? host : null;
    }

    /// <summary>
    /// Registers the host of a shown form, in place of any it had.
    /// </summary>
    /// <param name="host">The host, registered for its <see cref="IMenuHost.Root"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="host"/> is <see langword="null"/>.</exception>
    public static void Register(IMenuHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        hosts.AddOrUpdate(host.Root, host);
    }

    /// <summary>
    /// Unregisters a host, where it is still the one its root has.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <exception cref="ArgumentNullException"><paramref name="host"/> is <see langword="null"/>.</exception>
    public static void Unregister(IMenuHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (hosts.TryGetValue(host.Root, out var registered) && ReferenceEquals(registered, host))
            hosts.Remove(host.Root);
    }
}

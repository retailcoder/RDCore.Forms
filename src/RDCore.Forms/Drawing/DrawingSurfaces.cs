using System.Runtime.CompilerServices;
using RDCore.Forms.Model;

namespace RDCore.Forms.Drawing;

/// <summary>
/// The drawing surface of each shown form and PictureBox, by its component: where a program's drawing statements go.
/// </summary>
/// <remarks>
/// A form's view registers the surfaces of its root and its PictureBoxes while it is shown, and unregisters them when it is closed. A
/// component holds no reference to its surface, so a component no longer shown can be collected.
/// </remarks>
public static class DrawingSurfaces
{
    private static readonly ConditionalWeakTable<ComponentInstance, IDrawingSurface> surfaces = new();

    /// <summary>
    /// The surface a component draws on.
    /// </summary>
    /// <param name="component">The form's root or the PictureBox.</param>
    /// <returns>Its surface, or <see langword="null"/> where it is not shown, or has none.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="component"/> is <see langword="null"/>.</exception>
    public static IDrawingSurface? Of(ComponentInstance component)
    {
        ArgumentNullException.ThrowIfNull(component);
        return surfaces.TryGetValue(component, out var surface) ? surface : null;
    }

    /// <summary>
    /// Registers the surface of a shown component, in place of any it had.
    /// </summary>
    /// <param name="surface">The surface, registered for its <see cref="IDrawingSurface.Component"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="surface"/> is <see langword="null"/>.</exception>
    public static void Register(IDrawingSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        surfaces.AddOrUpdate(surface.Component, surface);
    }

    /// <summary>
    /// Unregisters a surface, where it is still the one its component has.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <exception cref="ArgumentNullException"><paramref name="surface"/> is <see langword="null"/>.</exception>
    public static void Unregister(IDrawingSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        if (surfaces.TryGetValue(surface.Component, out var registered) && ReferenceEquals(registered, surface))
            surfaces.Remove(surface.Component);
    }
}

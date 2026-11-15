using System.Collections;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Menus;

/// <summary>
/// A menu array: the items of a form that share a name and each have an index, with the members and the rules VB6 has for them at
/// run time.
/// </summary>
/// <remarks>
/// <para>
/// The elements are read from the form each time they are asked for, so those of the form's file and those a program loads are one
/// array. They are in the same menu, or all on the menu bar.
/// </para>
/// <para>
/// <see cref="Load"/> places a new element among the others in the order of their indexes: after the one with the highest index below
/// its own, or, with none below, before the one with the lowest index above. It copies the element that has the lowest index:
/// its <c>Caption</c>, <c>Checked</c>, <c>Enabled</c>, <c>Visible</c>, <c>Tag</c> and <c>HelpContextID</c>, as measured with VB6,
/// and its <c>CheckOnClick</c> and <c>Picture</c>, RDForms' own. It does not copy its <c>Shortcut</c>, its <c>WindowList</c> or the
/// items of its menu, so a copy of a menu is an item that opens none.
/// </para>
/// </remarks>
public sealed class FormMenuArray : IEnumerable<FormMenuItem>
{
    private const int HighestIndex = short.MaxValue;
    private static readonly PropertyDefinition[] Copied =
        [P.Caption, P.Checked, P.Enabled, P.Visible, P.Tag, P.HelpContextID, P.CheckOnClick, P.Picture];

    private readonly string name;

    internal FormMenuArray(FormMenu menu, string name)
    {
        Menu = menu;
        this.name = name;
    }

    /// <summary>
    /// The form's menus the array is part of.
    /// </summary>
    public FormMenu Menu { get; }

    /// <summary>
    /// The array's name: its elements'.
    /// </summary>
    public string Name => Elements.Count > 0 ? Elements[0].Component.Name : name;

    /// <summary>
    /// The elements loaded, in the order of their indexes.
    /// </summary>
    public IReadOnlyList<FormMenuItem> Elements =>
        [.. Menu.AllItems.Where(item => item.Index is not null && string.Equals(item.Component.Name, name, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Index)];

    /// <summary>
    /// How many elements are loaded.
    /// </summary>
    public int Count => Elements.Count;

    /// <summary>
    /// The lowest index of an element loaded.
    /// </summary>
    /// <exception cref="FormErrorException">No element is loaded: 340.</exception>
    public int LBound => Elements.FirstOrDefault()?.Index ?? throw new FormErrorException(FormError.ControlArrayElementDoesntExist(0));

    /// <summary>
    /// The highest index of an element loaded.
    /// </summary>
    /// <exception cref="FormErrorException">No element is loaded: 340.</exception>
    public int UBound => Elements.LastOrDefault()?.Index ?? throw new FormErrorException(FormError.ControlArrayElementDoesntExist(0));

    /// <summary>
    /// The element that has an index.
    /// </summary>
    /// <param name="index">The element's index.</param>
    /// <returns>The element.</returns>
    /// <exception cref="FormErrorException">No element of that index is loaded: 340, Control array element doesn't exist.</exception>
    public FormMenuItem this[int index] => Find(index) ?? throw new FormErrorException(FormError.ControlArrayElementDoesntExist(index));

    /// <summary>
    /// Whether an element of an index is loaded.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns><see langword="true"/> when the array has an element of that index.</returns>
    public bool IsLoaded(int index) => Find(index) is not null;

    /// <summary>
    /// Loads a new element, as a program's <c>Load</c> statement does, and raises <see cref="FormMenu.ItemLoaded"/>.
    /// </summary>
    /// <param name="index">The new element's index.</param>
    /// <returns>The element loaded.</returns>
    /// <exception cref="FormErrorException">
    /// <paramref name="index"/> is negative: 341, Invalid object array index. It is above 32767: 6, Overflow. An element of that index
    /// is loaded: 360, Object already loaded. Nothing changes.
    /// </exception>
    public FormMenuItem Load(int index)
    {
        if (index < 0)
            throw new FormErrorException(FormError.InvalidObjectArrayIndex);
        if (index > HighestIndex)
            throw new FormErrorException(FormError.Overflow);
        var elements = Elements;
        if (elements.Any(element => element.Index == index))
            throw new FormErrorException(FormError.ObjectAlreadyLoaded);
        var source = elements[0].Component;
        var container = source.Container!;
        var copy = new ComponentInstance(VBClasses.Menu, source.Name, index);
        foreach (var property in Copied)
        {
            if (source.IsSet(property))
                copy.TrySet(property, source.GetBoxed(property));
        }
        var below = elements.LastOrDefault(element => element.Index < index);
        var position = below is not null
            ? IndexIn(container, below.Component) + 1
            : IndexIn(container, elements.First(element => element.Index > index).Component);
        copy.SetContainer(container, position);
        var item = FormMenuItem.Of(copy);
        Menu.Loaded(item, container);
        return item;
    }

    /// <summary>
    /// Unloads an element a program loaded, as a program's <c>Unload</c> statement does, and raises <see cref="FormMenu.ItemUnloaded"/>.
    /// </summary>
    /// <param name="index">The element's index.</param>
    /// <exception cref="FormErrorException">
    /// No element of that index is loaded: 340, Control array element doesn't exist. The element is one of the form's file: 362, Can't
    /// unload controls created at design time. It is the only visible item of its menu: 361, Can't load or unload this object. Nothing
    /// changes.
    /// </exception>
    /// <remarks>
    /// The last visible item of the menu bar can be unloaded, and the bar goes with it.
    /// </remarks>
    public void Unload(int index)
    {
        var item = this[index];
        if (!Menu.IsLoadedAtRunTime(item))
            throw new FormErrorException(FormError.CantUnloadControlsCreatedAtDesignTime);
        if (item.Visible && item.Parent is { } menu && menu.Items.Count(other => other.Visible) == 1)
            throw new FormErrorException(FormError.CantLoadOrUnloadThisObject);
        var container = item.Component.Container!;
        item.Component.SetContainer(null);
        Menu.Unloaded(item, container);
    }

    /// <summary>
    /// The elements loaded, in the order of their indexes, as a program's <c>For Each</c> goes through them.
    /// </summary>
    /// <returns>The enumerator.</returns>
    public IEnumerator<FormMenuItem> GetEnumerator() => Elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private FormMenuItem? Find(int index) => Elements.FirstOrDefault(element => element.Index == index);

    private static int IndexIn(ComponentInstance container, ComponentInstance component)
    {
        for (var i = 0; i < container.Contents.Count; i++)
        {
            if (ReferenceEquals(container.Contents[i], component))
                return i;
        }
        return container.Contents.Count;
    }
}

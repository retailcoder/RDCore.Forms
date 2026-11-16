using System.Collections;

namespace RDCore.Forms.Model;

/// <summary>
/// A control array: the controls of a form that share a name and each have an index, with the members and the rules VB6 has for them at
/// run time. Menu items are in menu arrays (<see cref="Menus.FormMenuArray"/>).
/// </summary>
/// <remarks>
/// <para>
/// The elements are read from the form each time they are asked for, so those of the form's file and those a program loads are one
/// array.
/// </para>
/// <para>
/// <see cref="Load"/> copies the element that has the lowest index, with the values it has at the time: every property set on it but
/// <c>Visible</c>, which is <see langword="false"/>, and <c>TabIndex</c>, which is the next after the form's others. The copy is placed
/// in the same container, last, and it holds none of the controls the element holds. A program's <c>Controls</c> collection has it after
/// the controls of the form's file, in the order the program loaded it (<see cref="FormDefinition.Controls"/>).
/// </para>
/// </remarks>
public sealed class FormControlArray : IEnumerable<ComponentInstance>
{
    private const int HighestIndex = short.MaxValue;

    private readonly string name;

    internal FormControlArray(FormDefinition form, string name)
    {
        Form = form;
        this.name = name;
    }

    /// <summary>
    /// The form the array is part of.
    /// </summary>
    public FormDefinition Form { get; }

    /// <summary>
    /// The array's name: its elements'.
    /// </summary>
    public string Name => Elements.Count > 0 ? Elements[0].Name : name;

    /// <summary>
    /// The elements loaded, in the order of their indexes.
    /// </summary>
    public IReadOnlyList<ComponentInstance> Elements =>
        [.. Form.Root.Descendants().Where(component => IsElement(component, name)).OrderBy(component => component.Index)];

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
    public ComponentInstance this[int index] => Find(index) ?? throw new FormErrorException(FormError.ControlArrayElementDoesntExist(index));

    /// <summary>
    /// Whether an element of an index is loaded.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns><see langword="true"/> when the array has an element of that index.</returns>
    public bool IsLoaded(int index) => Find(index) is not null;

    /// <summary>
    /// Loads a new element, as a program's <c>Load</c> statement does, and raises <see cref="FormDefinition.ControlAdded"/>.
    /// </summary>
    /// <param name="index">The new element's index.</param>
    /// <returns>The element loaded.</returns>
    /// <exception cref="FormErrorException">
    /// <paramref name="index"/> is negative: 341, Invalid control array index. It is above 32767: 6, Overflow. An element of that index
    /// is loaded: 360, Object already loaded. No element is loaded to copy (the form has none of the array's name left): 340. Nothing
    /// changes.
    /// </exception>
    public ComponentInstance Load(int index)
    {
        if (index < 0)
            throw new FormErrorException(FormError.InvalidControlArrayIndex);
        if (index > HighestIndex)
            throw new FormErrorException(FormError.Overflow);
        var elements = Elements;
        if (elements.Any(element => element.Index == index))
            throw new FormErrorException(FormError.ObjectAlreadyLoaded);
        if (elements.Count == 0)
            throw new FormErrorException(FormError.ControlArrayElementDoesntExist(index));
        return Form.LoadElement(elements[0], index);
    }

    /// <summary>
    /// Unloads an element a program loaded, as a program's <c>Unload</c> statement does, and raises <see cref="FormDefinition.ControlRemoved"/>.
    /// The form's other <c>TabIndex</c> values close up over the element's.
    /// </summary>
    /// <param name="index">The element's index.</param>
    /// <exception cref="FormErrorException">
    /// No element of that index is loaded: 340, Control array element doesn't exist. The element is one of the form's file: 362, Can't
    /// unload controls created at design time. Nothing changes.
    /// </exception>
    public void Unload(int index)
    {
        var element = this[index];
        if (!Form.IsAddedAtRunTime(element))
            throw new FormErrorException(FormError.CantUnloadControlsCreatedAtDesignTime);
        Form.UnloadElement(element);
    }

    /// <summary>
    /// The elements loaded when the enumeration starts, in the order of their indexes, as a program's <c>For Each</c> goes over them.
    /// </summary>
    /// <returns>An enumerator of the elements.</returns>
    public IEnumerator<ComponentInstance> GetEnumerator() => Elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    // a control, not a menu item, that is an element of the array of a name.
    internal static bool IsElement(ComponentInstance component, string name)
        => component.Index is not null && component.Class.Kind == ComponentKind.Control
        && string.Equals(component.Name, name, StringComparison.OrdinalIgnoreCase);

    private ComponentInstance? Find(int index) => Elements.FirstOrDefault(element => element.Index == index);
}

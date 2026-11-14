// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model;

/// <summary>
/// A component of a form as its designer holds it: a form's root, a control or a menu item, with the properties set on it and the
/// components it contains.
/// </summary>
/// <remarks>
/// A property that is not set has its class's default (<see cref="ComponentClass.DefaultOf(PropertyDefinition)"/>). Containment is
/// kept in both directions, by <see cref="SetContainer"/> only, so <see cref="Container"/> and <see cref="Contents"/> always agree.
/// </remarks>
public sealed class ComponentInstance
{
    private readonly Dictionary<PropertyDefinition, object?> values = [];
    private readonly List<ComponentInstance> contents = [];
    private string name;

    /// <summary>
    /// Creates an instance of <paramref name="componentClass"/>, with no property set and no container.
    /// </summary>
    /// <param name="componentClass">The instance's class.</param>
    /// <param name="name">The instance's name.</param>
    /// <param name="index">The instance's index in its control array, or <see langword="null"/> when it is not in one.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or white space.</exception>
    public ComponentInstance(ComponentClass componentClass, string name, int? index = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Class = componentClass;
        this.name = name;
        Index = index;
    }

    /// <summary>
    /// Raised after a property is set or reset, whether or not its value differs.
    /// </summary>
    public event EventHandler<ComponentPropertyChangedEventArgs>? PropertyChanged;

    /// <summary>
    /// The instance's class.
    /// </summary>
    public ComponentClass Class { get; }

    /// <summary>
    /// The instance's name, as code refers to it. Every element of a control array has the array's name.
    /// </summary>
    /// <exception cref="ArgumentException">The name set is empty or white space.</exception>
    public string Name
    {
        get => name;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            name = value;
        }
    }

    /// <summary>
    /// The instance's index in its control array, or <see langword="null"/> when it is not in one.
    /// </summary>
    public int? Index { get; set; }

    /// <summary>
    /// The component this one is placed in, or <see langword="null"/> for a form's root and for a component placed in none.
    /// </summary>
    public ComponentInstance? Container { get; private set; }

    /// <summary>
    /// The components placed in this one, in the order they were placed (a designer file's order, for a form read from one):
    /// controls, which a designer draws in that order, and, for a root or a menu, its menu items.
    /// </summary>
    public IReadOnlyList<ComponentInstance> Contents => contents;

    /// <summary>
    /// The properties a designer file gave this component that its class does not model, kept as they were read.
    /// </summary>
    public List<UnmodelledProperty> UnmodelledProperties { get; } = [];

    /// <summary>
    /// The components a designer file placed in this one whose class the model does not know, kept as they were read.
    /// </summary>
    public List<UnmodelledComponent> UnmodelledContents { get; } = [];

    /// <summary>
    /// Places this component in <paramref name="container"/>, and takes it out of the one it was in.
    /// </summary>
    /// <param name="container">The new container, or <see langword="null"/> to take the component out of its container.</param>
    /// <param name="index">Where among the container's contents to place it; a negative index, or one at or past the end, places it last.</param>
    /// <exception cref="InvalidOperationException">
    /// The placement is not one a form can hold: a root placed in anything, a menu item placed anywhere but in a root or another menu
    /// item, a control placed in a component that holds no controls, or a component placed in itself or in its own contents.
    /// </exception>
    public void SetContainer(ComponentInstance? container, int index = -1)
    {
        if (container is not null)
        {
            var placeable = Class.Kind switch
            {
                ComponentKind.Root => false,
                ComponentKind.Menu => container.Class.Kind is ComponentKind.Root or ComponentKind.Menu,
                _ => container.Class.IsContainer,
            };
            if (!placeable)
                throw new InvalidOperationException($"{Class.ClassName} {Name} cannot be placed in {container.Class.ClassName} {container.Name}.");
            for (var ancestor = container; ancestor is not null; ancestor = ancestor.Container)
            {
                if (ReferenceEquals(ancestor, this))
                    throw new InvalidOperationException($"{Name} cannot be placed in itself or in its own contents.");
            }
        }

        Container?.contents.Remove(this);
        Container = container;
        if (container is null)
            return;
        if (index < 0 || index >= container.contents.Count)
            container.contents.Add(this);
        else
            container.contents.Insert(index, this);
    }

    /// <summary>
    /// Every component in this one's contents, and in theirs, depth first, each component's contents in the order of its <see cref="Contents"/>.
    /// </summary>
    /// <returns>The descendants of this component, without this component.</returns>
    public IEnumerable<ComponentInstance> Descendants()
    {
        // an explicit stack, so a deep nesting costs neither the call stack nor a chain of nested enumerators.
        var pending = new Stack<IEnumerator<ComponentInstance>>();
        pending.Push(contents.GetEnumerator());
        try
        {
            while (pending.TryPeek(out var siblings))
            {
                if (!siblings.MoveNext())
                {
                    pending.Pop().Dispose();
                    continue;
                }

                var child = siblings.Current;
                yield return child;
                pending.Push(child.contents.GetEnumerator());
            }
        }
        finally
        {
            while (pending.TryPop(out var siblings))
                siblings.Dispose();
        }
    }

    /// <summary>
    /// The value of <paramref name="property"/>: the value set, or the class's default.
    /// </summary>
    /// <typeparam name="T">The property's value type.</typeparam>
    /// <param name="property">The property.</param>
    /// <returns>The property's value.</returns>
    /// <exception cref="ArgumentException">The instance's class does not have <paramref name="property"/>.</exception>
    public T Get<T>(PropertyDefinition<T> property)
    {
        RequireProperty(property);
        if (property.Read is { } read)
            return read(this);
        return values.TryGetValue(property, out var value) ? (T)value! : Class.DefaultOf(property);
    }

    /// <summary>
    /// The value of <paramref name="property"/>, boxed: the value set, or the class's default.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns>The property's value.</returns>
    /// <exception cref="ArgumentException">The instance's class does not have <paramref name="property"/>.</exception>
    public object? GetBoxed(PropertyDefinition property)
    {
        RequireProperty(property);
        if (property.IsDerived)
            return property.Derived(this);
        return values.TryGetValue(property, out var value) ? value : Class.DefaultOf(property);
    }

    /// <summary>
    /// The value of <paramref name="property"/>, boxed, where the instance has one.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The property's value; <see langword="null"/> where the instance has none.</param>
    /// <returns>
    /// <see langword="false"/> where the class does not have the property, or where the instance's other properties leave it without
    /// one: a ComboBox whose style is Dropdown List has no edit box, and no <c>SelStart</c>. VB refuses a program's read of it with 380,
    /// Invalid property value.
    /// </returns>
    public bool TryGet(PropertyDefinition property, out object? value)
    {
        var present = Class.Has(property) && property.IsPresent(this);
        value = present ? GetBoxed(property) : null;
        return present;
    }

    /// <summary>
    /// Whether <paramref name="property"/> is set on this instance, rather than taken from its class's default.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns><see langword="true"/> when the property is set.</returns>
    public bool IsSet(PropertyDefinition property) => values.ContainsKey(property);

    /// <summary>
    /// The properties set on this instance, with their values.
    /// </summary>
    public IReadOnlyDictionary<PropertyDefinition, object?> SetValues => values;

    /// <summary>
    /// Sets <paramref name="property"/> to <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The property's value type.</typeparam>
    /// <param name="property">The property.</param>
    /// <param name="value">The value.</param>
    /// <returns>This instance.</returns>
    /// <exception cref="ArgumentException">The instance's class does not have <paramref name="property"/>.</exception>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <see langword="null"/> for a property whose assignment sets others, such as <c>SelText</c>.
    /// </exception>
    /// <remarks>
    /// An assignment can set other properties with it: a TextBox's <c>SelText</c> sets its <c>Text</c>, <c>SelStart</c> and
    /// <c>SelLength</c>. <see cref="PropertyChanged"/> is raised for each, once every value of the step is stored, with
    /// <paramref name="property"/> as <see cref="ComponentPropertyChangedEventArgs.AssignedProperty"/>.
    /// </remarks>
    public ComponentInstance Set<T>(PropertyDefinition<T> property, T value)
    {
        RequireProperty(property);
        Assign(property, value);
        return this;
    }

    /// <summary>
    /// Sets <paramref name="property"/> to <paramref name="value"/>, when the class has the property, the instance has it, and the
    /// property can hold the value.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <param name="value">The value.</param>
    /// <returns>
    /// <see langword="true"/> when the value was set; <see langword="false"/>, with nothing changed, otherwise. A program's assignment
    /// that is refused is VB's 380, Invalid property value.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value"/> is <see langword="null"/> for a property whose assignment sets others, such as <c>SelText</c>.
    /// </exception>
    /// <remarks>
    /// An instance that has no value of the property refuses it (see <see cref="TryGet"/>). The assignment sets other properties as
    /// <see cref="Set{T}"/> does.
    /// </remarks>
    public bool TrySet(PropertyDefinition property, object? value)
    {
        if (!Class.Has(property) || !property.IsPresent(this) || !property.Accepts(value))
            return false;
        Assign(property, value);
        return true;
    }

    // A value as a form's file holds it, which a component loads as it is: a TextBox's text longer than its MaxLength is not cut.
    internal bool TryRead(PropertyDefinition property, object? value)
    {
        if (!Class.Has(property) || !property.Accepts(value))
            return false;
        Store([(property, value)], property);
        return true;
    }

    /// <summary>
    /// Clears <paramref name="property"/>, so it has its class's default again.
    /// </summary>
    /// <param name="property">The property.</param>
    /// <returns><see langword="true"/> when the property was set.</returns>
    public bool Reset(PropertyDefinition property)
    {
        if (!values.TryGetValue(property, out var before))
            return false;
        values.Remove(property);
        PropertyChanged?.Invoke(this, new ComponentPropertyChangedEventArgs(property, before, Class.DefaultOf(property)));
        return true;
    }

    /// <summary>
    /// The instance's name, with its index when it is in a control array.
    /// </summary>
    /// <returns>The instance's name, such as <c>Command1</c> or <c>Option1(2)</c>.</returns>
    public override string ToString() => Index is { } index ? $"{Name}({index})" : Name;

    private void Assign(PropertyDefinition property, object? value)
    {
        foreach (var step in property.Assignment(this, value))
            Store(step, property);
    }

    // The values are stored together, so the handler of each one's PropertyChanged sees all of them.
    private void Store(IReadOnlyList<(PropertyDefinition Property, object? Value)> step, PropertyDefinition assigned)
    {
        var before = new object?[step.Count];
        for (var i = 0; i < step.Count; i++)
            before[i] = GetBoxed(step[i].Property);
        foreach (var (property, value) in step)
            values[property] = value;
        for (var i = 0; i < step.Count; i++)
            PropertyChanged?.Invoke(this, new ComponentPropertyChangedEventArgs(step[i].Property, before[i], step[i].Value, assigned));
    }

    private void RequireProperty(PropertyDefinition property)
    {
        if (!Class.Has(property))
            throw new ArgumentException($"{Class.ClassName} has no property {property.Name}.", nameof(property));
    }
}

/// <summary>
/// The data of <see cref="ComponentInstance.PropertyChanged"/>.
/// </summary>
/// <param name="property">The property whose value changed.</param>
/// <param name="before">The property's value before the change.</param>
/// <param name="after">The property's value after the change.</param>
/// <param name="assigned">
/// The property whose assignment made the change, or <see langword="null"/> for <paramref name="property"/> itself.
/// </param>
public sealed class ComponentPropertyChangedEventArgs(PropertyDefinition property, object? before, object? after, PropertyDefinition? assigned = null)
    : EventArgs
{
    /// <summary>
    /// The property whose value changed.
    /// </summary>
    public PropertyDefinition Property { get; } = property;

    /// <summary>
    /// The property whose assignment made the change: <see cref="Property"/> itself, or another that sets it, such as a TextBox's
    /// <c>SelText</c> for the <c>Text</c> it replaces.
    /// </summary>
    public PropertyDefinition AssignedProperty { get; } = assigned ?? property;

    /// <summary>
    /// The property's value before the change.
    /// </summary>
    public object? Before { get; } = before;

    /// <summary>
    /// The property's value after the change.
    /// </summary>
    public object? After { get; } = after;
}

/// <summary>
/// A property a designer file gave a component that its class does not model, kept as it was read.
/// </summary>
/// <param name="Name">The property's name, as the file spells it.</param>
/// <param name="Text">The property's value as the file wrote it: one line, or a block of lines for a property that has its own.</param>
public sealed record UnmodelledProperty(string Name, string Text);

/// <summary>
/// A component a designer file placed in another whose class the model does not know, such as an ActiveX control, kept as it was read.
/// </summary>
/// <param name="TypeName">The component's qualified type name, such as <c>MSComctlLib.ListView</c>.</param>
/// <param name="Name">The component's name.</param>
/// <param name="Position">How many of its container's modelled contents the file holds before it.</param>
/// <param name="Text">The component's block as the file wrote it, its own contents included.</param>
public sealed record UnmodelledComponent(string TypeName, string Name, int Position, string Text);

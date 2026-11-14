// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model;

/// <summary>
/// What a component class is to the form that holds it.
/// </summary>
public enum ComponentKind
{
    /// <summary>
    /// A control: a component placed on a form or inside a container.
    /// </summary>
    Control,
    /// <summary>
    /// A menu item. A menu is not drawn inside a container: its children are its submenu's items.
    /// </summary>
    Menu,
    /// <summary>
    /// The root of a designer: a form, a UserControl or a PropertyPage.
    /// </summary>
    Root,
}

/// <summary>
/// The metadata of a component class, such as VB's <c>VB.CommandButton</c>: its type name, the properties and events it
/// has, and how it sits on a form.
/// </summary>
/// <remarks>
/// A class has at most one property of a given name, compared without regard to case: <c>Value</c> is a Boolean on an
/// OptionButton and an Integer on a CheckBox, and each class names the definition it has.
/// </remarks>
public sealed class ComponentClass
{
    private readonly Dictionary<string, PropertyDefinition> propertiesByName;
    private readonly Dictionary<string, EventDefinition> eventsByName;
    private readonly Dictionary<string, MethodDefinition> methodsByName;
    private readonly Dictionary<PropertyDefinition, object?> defaults;
    private readonly HashSet<PropertyDefinition> notAvailable;

    /// <summary>
    /// Creates a component class.
    /// </summary>
    /// <param name="typeName">The class's qualified type name, such as <c>VB.CommandButton</c>.</param>
    /// <param name="defaultName">The name a designer gives the first instance it adds, less its number, such as <c>Command</c>.</param>
    /// <param name="kind">What the class is to the form that holds it.</param>
    /// <param name="properties">The class's properties, in the order a designer lists them.</param>
    /// <param name="events">The class's events.</param>
    /// <param name="defaults">The defaults this class gives to some of its properties in place of the definitions' own.</param>
    /// <param name="isContainer">Whether an instance can hold controls: a form, a Frame, a PictureBox.</param>
    /// <param name="isWindowless">
    /// Whether the control is drawn on its container's own surface rather than in a window of its own, as VB's Label, Image, Shape
    /// and Line are. Such a control is drawn under every windowed control of its container.
    /// </param>
    /// <param name="hasExtent">
    /// Whether an instance has a width and a height. A Timer has a place in the designer and no size, and a running program
    /// cannot see it.
    /// </param>
    /// <param name="methods">The class's methods; none where it is <see langword="null"/>.</param>
    /// <param name="notAvailableAtRunTime">
    /// The properties a program can neither read nor set on an instance of this class, beyond those whose definition says so, such as
    /// the <c>Left</c> and <c>Top</c> of a Timer; none where it is <see langword="null"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="typeName"/> or <paramref name="defaultName"/> is empty or white space; two properties, two events or two methods share a
    /// name; a default is given for a property the class does not have, or is not a value the property can hold; or
    /// <paramref name="notAvailableAtRunTime"/> names a property the class does not have.
    /// </exception>
    public ComponentClass(string typeName, string defaultName, ComponentKind kind,
        IReadOnlyList<PropertyDefinition> properties, IReadOnlyList<EventDefinition> events,
        IReadOnlyDictionary<PropertyDefinition, object?>? defaults = null,
        bool isContainer = false, bool isWindowless = false, bool hasExtent = true, IReadOnlyList<MethodDefinition>? methods = null,
        IReadOnlyList<PropertyDefinition>? notAvailableAtRunTime = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultName);
        TypeName = typeName;
        DefaultName = defaultName;
        Kind = kind;
        IsContainer = isContainer;
        IsWindowless = isWindowless;
        HasExtent = hasExtent;

        Properties = [.. properties];
        propertiesByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (var property in Properties)
        {
            if (!propertiesByName.TryAdd(property.Name, property))
                throw new ArgumentException($"{typeName} has two properties named {property.Name}.", nameof(properties));
        }

        Events = [.. events];
        eventsByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (var @event in Events)
        {
            if (!eventsByName.TryAdd(@event.Name, @event))
                throw new ArgumentException($"{typeName} has two events named {@event.Name}.", nameof(events));
        }

        Methods = [.. methods ?? []];
        methodsByName = new(StringComparer.OrdinalIgnoreCase);
        foreach (var method in Methods)
        {
            if (!methodsByName.TryAdd(method.Name, method))
                throw new ArgumentException($"{typeName} has two methods named {method.Name}.", nameof(methods));
        }

        this.defaults = [];
        foreach (var (property, value) in defaults ?? new Dictionary<PropertyDefinition, object?>())
        {
            if (!ReferenceEquals(Property(property.Name), property))
                throw new ArgumentException($"{typeName} has no property {property.Name} to give a default to.", nameof(defaults));
            if (!property.Accepts(value))
                throw new ArgumentException($"{value} is not a value of {typeName}.{property.Name}.", nameof(defaults));
            this.defaults[property] = value;
        }

        notAvailable = new(ReferenceEqualityComparer.Instance);
        foreach (var property in notAvailableAtRunTime ?? [])
        {
            if (!Has(property))
                throw new ArgumentException($"{typeName} has no property {property.Name} to make unavailable at run time.", nameof(notAvailableAtRunTime));
            notAvailable.Add(property);
        }
    }

    /// <summary>
    /// The class's qualified type name, such as <c>VB.CommandButton</c>.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// The class's name without its library, such as <c>CommandButton</c>.
    /// </summary>
    public string ClassName => TypeName[(TypeName.LastIndexOf('.') + 1)..];

    /// <summary>
    /// The name a designer gives the first instance it adds, less its number, such as <c>Command</c> for <c>Command1</c>.
    /// </summary>
    public string DefaultName { get; }

    /// <summary>
    /// What the class is to the form that holds it.
    /// </summary>
    public ComponentKind Kind { get; }

    /// <summary>
    /// Whether an instance can hold controls.
    /// </summary>
    public bool IsContainer { get; }

    /// <summary>
    /// Whether the control is drawn on its container's surface, under every windowed control of that container.
    /// </summary>
    public bool IsWindowless { get; }

    /// <summary>
    /// Whether an instance has a width and a height.
    /// </summary>
    public bool HasExtent { get; }

    /// <summary>
    /// The class's properties, in the order a designer lists them.
    /// </summary>
    public IReadOnlyList<PropertyDefinition> Properties { get; }

    /// <summary>
    /// The class's events.
    /// </summary>
    public IReadOnlyList<EventDefinition> Events { get; }

    /// <summary>
    /// The class's property of a given name, compared without regard to case.
    /// </summary>
    /// <param name="name">The property's name.</param>
    /// <returns>The property, or <see langword="null"/> when the class has none of that name.</returns>
    public PropertyDefinition? Property(string name) => propertiesByName.GetValueOrDefault(name);

    /// <summary>
    /// The class's event of a given name, compared without regard to case.
    /// </summary>
    /// <param name="name">The event's name.</param>
    /// <returns>The event, or <see langword="null"/> when the class has none of that name.</returns>
    public EventDefinition? Event(string name) => eventsByName.GetValueOrDefault(name);

    /// <summary>
    /// The class's methods: what a program's code calls on an instance, such as <c>SetFocus</c> or <c>Move</c>.
    /// </summary>
    public IReadOnlyList<MethodDefinition> Methods { get; }

    /// <summary>
    /// The class's method of a given name, compared without regard to case.
    /// </summary>
    /// <param name="name">The method's name.</param>
    /// <returns>The method, or <see langword="null"/> when the class has none of that name.</returns>
    public MethodDefinition? Method(string name) => methodsByName.GetValueOrDefault(name);

    /// <summary>
    /// Whether the class has <paramref name="property"/>, the definition compared by reference.
    /// </summary>
    /// <param name="property">The property definition.</param>
    /// <returns><see langword="true"/> when the class has the property.</returns>
    public bool Has(PropertyDefinition property) => ReferenceEquals(Property(property.Name), property);

    /// <summary>
    /// The value an instance of this class has for <paramref name="property"/> when nothing sets it: the class's own default, or
    /// the definition's.
    /// </summary>
    /// <param name="property">The property definition.</param>
    /// <returns>The default value.</returns>
    public object? DefaultOf(PropertyDefinition property) =>
        defaults.TryGetValue(property, out var value) ? value : property.BoxedDefault;

    /// <summary>
    /// The value an instance of this class has for <paramref name="property"/> when nothing sets it.
    /// </summary>
    /// <typeparam name="T">The property's value type.</typeparam>
    /// <param name="property">The property definition.</param>
    /// <returns>The default value.</returns>
    public T DefaultOf<T>(PropertyDefinition<T> property) =>
        defaults.TryGetValue(property, out var value) ? (T)value! : property.Default;

    /// <summary>
    /// Whether a program can neither read nor set <paramref name="property"/> on an instance of this class: VB's 393 on a read and 382
    /// on an assignment.
    /// </summary>
    /// <param name="property">The property definition.</param>
    /// <returns>
    /// <see langword="true"/> when the definition says so (<see cref="PropertyDefinition.NotAvailableAtRunTime"/>) or the class does.
    /// </returns>
    public bool IsNotAvailableAtRunTime(PropertyDefinition property) => property.NotAvailableAtRunTime || notAvailable.Contains(property);

    /// <summary>
    /// Whether a program cannot set <paramref name="property"/> on an instance of this class: VB's 382 on an assignment.
    /// </summary>
    /// <param name="property">The property definition.</param>
    /// <returns>
    /// <see langword="true"/> when the property is <see cref="PropertyDefinition.ReadOnlyAtRunTime"/>, or is not available at run time
    /// (<see cref="IsNotAvailableAtRunTime"/>).
    /// </returns>
    public bool IsReadOnlyAtRunTime(PropertyDefinition property) => property.ReadOnlyAtRunTime || IsNotAvailableAtRunTime(property);

    /// <summary>
    /// The class's type name.
    /// </summary>
    /// <returns>The class's type name.</returns>
    public override string ToString() => TypeName;
}

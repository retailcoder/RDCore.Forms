// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model;

/// <summary>
/// The group a designer's property grid shows a property under.
/// </summary>
public enum PropertyCategory
{
    /// <summary>
    /// A property with no more specific group.
    /// </summary>
    Misc,
    /// <summary>
    /// How the component looks: its colours, font, border and caption.
    /// </summary>
    Appearance,
    /// <summary>
    /// How the component responds: whether it is enabled, takes focus, or accepts input.
    /// </summary>
    Behavior,
    /// <summary>
    /// Where the component is and how big it is.
    /// </summary>
    Position,
    /// <summary>
    /// The items a list control holds.
    /// </summary>
    List,
}

/// <summary>
/// A property of a component class: its name, its value type, and its default.
/// </summary>
/// <remarks>
/// A definition is shared by every class that has the property. A class can give it a default of its own
/// (<see cref="ComponentClass.DefaultOf(PropertyDefinition)"/>): a TextBox's <c>BackColor</c> defaults to the window colour
/// where most controls default to the button face. Names compare without regard to case, as VB does.
/// </remarks>
public abstract class PropertyDefinition
{
    /// <summary>
    /// Creates a property definition.
    /// </summary>
    /// <param name="name">The property's name, as code and a designer file spell it.</param>
    /// <param name="category">The group a designer shows the property under.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or white space.</exception>
    private protected PropertyDefinition(string name, PropertyCategory category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Category = category;
    }

    /// <summary>
    /// The property's name, as code and a designer file spell it.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The group a designer shows the property under.
    /// </summary>
    public PropertyCategory Category { get; }

    /// <summary>
    /// The CLR type of the property's values. A <see langword="null"/> value is allowed for a reference type and a nullable value type.
    /// </summary>
    public abstract Type ValueType { get; }

    /// <summary>
    /// The default value the definition declares, before any class gives it one of its own.
    /// </summary>
    public abstract object? BoxedDefault { get; }

    /// <summary>
    /// Whether a program cannot set the property, which only a designer and a form's file set. VB refuses a program's assignment
    /// with <see cref="FormError.SetNotSupportedAtRuntime"/>, and early-bound code that assigns it does not compile.
    /// </summary>
    public bool ReadOnlyAtRunTime { get; init; }

    /// <summary>
    /// Whether a program can neither read nor set the property, which only a designer and a form's file set. VB refuses a program's
    /// read with <see cref="FormError.GetNotSupportedAtRuntime"/> and its assignment with <see cref="FormError.SetNotSupportedAtRuntime"/>.
    /// </summary>
    /// <remarks>
    /// A class can make a property it shares with other classes unavailable on its own instances: ask the class, with
    /// <see cref="ComponentClass.IsNotAvailableAtRunTime"/> and <see cref="ComponentClass.IsReadOnlyAtRunTime"/>.
    /// </remarks>
    public bool NotAvailableAtRunTime { get; init; }

    /// <summary>
    /// Whether <paramref name="value"/> can be held by this property: a value of <see cref="ValueType"/> that is among the
    /// property's valid values, or <see langword="null"/> where the type allows it.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><see langword="true"/> when the property can hold the value.</returns>
    /// <remarks>
    /// A program's assignment of a value the property cannot hold is VB's 380, Invalid property value.
    /// </remarks>
    public bool Accepts(object? value) =>
        value is null
            ? !ValueType.IsValueType || Nullable.GetUnderlyingType(ValueType) is not null
            : ValueType.IsInstanceOfType(value) && IsValid(value);

    // Whether a value of the property's type is among its valid values.
    private protected abstract bool IsValid(object value);

    // The value a component holds when the property is set to a value.
    internal abstract object? Coerced(ComponentInstance component, object? value);

    /// <summary>
    /// The property's name.
    /// </summary>
    /// <returns>The property's name.</returns>
    public override string ToString() => Name;
}

/// <summary>
/// A property of a component class whose values are of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The CLR type of the property's values.</typeparam>
/// <param name="name">The property's name, as code and a designer file spell it.</param>
/// <param name="category">The group a designer shows the property under.</param>
/// <param name="defaultValue">The value a component has when nothing sets the property.</param>
public sealed class PropertyDefinition<T>(string name, PropertyCategory category, T defaultValue = default!)
    : PropertyDefinition(name, category)
{
    /// <summary>
    /// The value a component has when nothing sets the property, unless its class gives the property a default of its own.
    /// </summary>
    public T Default { get; } = defaultValue;

    /// <summary>
    /// Which values of its type the property can hold, or <see langword="null"/> where it can hold all of them.
    /// </summary>
    public Func<T, bool>? Valid { get; init; }

    /// <summary>
    /// What a component holds when the property is set to a value, from the value and the component's other properties, or
    /// <see langword="null"/> for the value as set.
    /// </summary>
    public Func<ComponentInstance, T, T>? Coerce { get; init; }

    /// <inheritdoc/>
    public override Type ValueType => typeof(T);

    /// <inheritdoc/>
    public override object? BoxedDefault => Default;

    private protected override bool IsValid(object value) => Valid is null || Valid((T)value);

    internal override object? Coerced(ComponentInstance component, object? value) =>
        Coerce is not null && value is T typed ? Coerce(component, typed) : value;
}

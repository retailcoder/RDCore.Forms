namespace RDCore.Forms.Model;

/// <summary>
/// A method a component class has, or a property that a form's window answers for it: its name and its parameters, as a program's code
/// calls it.
/// </summary>
/// <remarks>
/// <para>
/// The definition describes the call. What the method does is the form's: a shown form's window runs it (see
/// <c>RDFormsFormWindow.Invoke</c> in RDCore.Forms.Avalonia).
/// </para>
/// <para>
/// A property here is one whose value the window's control holds, such as a ListBox's <c>ListIndex</c>, or one that
/// takes arguments, such as its <c>List(Index)</c>. <see cref="Value"/> gives its type. The window reads it with <c>Invoke</c>, and
/// assigns it with <c>Assign</c> unless it <see cref="IsReadOnly"/>. Where the class has a model property of the same name, such as a
/// ListBox's <c>List</c>, a running program reaches this one, and a designer the model's.
/// </para>
/// </remarks>
public sealed class MethodDefinition
{
    /// <summary>
    /// Creates a method definition.
    /// </summary>
    /// <param name="name">The method's name.</param>
    /// <param name="parameters">The method's parameters, in order.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="name"/> is empty or white space, or a required parameter follows an optional one.
    /// </exception>
    public MethodDefinition(string name, params IReadOnlyList<MethodParameter> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        for (var i = 1; i < parameters.Count; i++)
        {
            if (parameters[i - 1].IsOptional && !parameters[i].IsOptional)
                throw new ArgumentException($"{name}'s required parameter {parameters[i].Name} follows an optional one.", nameof(parameters));
        }
        Name = name;
        Parameters = [.. parameters];
    }

    /// <summary>
    /// The method's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The method's parameters, in the order a call gives them.
    /// </summary>
    public IReadOnlyList<MethodParameter> Parameters { get; }

    /// <summary>
    /// The number of arguments a call must give: the parameters before the first optional one.
    /// </summary>
    public int RequiredCount => Parameters.TakeWhile(parameter => !parameter.IsOptional).Count();

    /// <summary>
    /// For a property, its value: the type a read returns, and the type an assignment's value is coerced to. <see langword="null"/>
    /// for a method.
    /// </summary>
    public MethodParameter? Value { get; init; }

    /// <summary>
    /// Whether the definition is a property's: whether it has a <see cref="Value"/>.
    /// </summary>
    public bool IsProperty => Value is not null;

    /// <summary>
    /// Whether a program can only read the property. An assignment is error 383, after its value is coerced to the property's type.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// The method's name.
    /// </summary>
    /// <returns>The method's name.</returns>
    public override string ToString() => Name;
}

/// <summary>
/// A parameter of a method.
/// </summary>
/// <param name="Name">The parameter's name, as a named argument gives it, such as <c>Left</c>.</param>
/// <param name="TypeName">
/// The declared type of the parameter, as VB names it, such as <c>Single</c>: a program's argument is coerced to it before the method
/// runs. A <c>Variant</c> parameter takes the argument as it is.
/// </param>
/// <param name="IsOptional">Whether a call may leave the argument out.</param>
public sealed record MethodParameter(string Name, string TypeName, bool IsOptional = false);

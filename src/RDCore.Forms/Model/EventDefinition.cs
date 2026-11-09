// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

namespace RDCore.Forms.Model;

/// <summary>
/// An event a component class raises: its name and its parameters, as a handler declares them.
/// </summary>
/// <remarks>
/// A component raises an event with the event's name and its arguments. Which procedure handles it, such as <c>Command1_Click</c>
/// or a <c>WithEvents</c> variable's handler, is for the code that runs the form to decide.
/// </remarks>
public sealed class EventDefinition
{
    /// <summary>
    /// Creates an event definition.
    /// </summary>
    /// <param name="name">The event's name.</param>
    /// <param name="parameters">The event's parameters, in order.</param>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or white space.</exception>
    public EventDefinition(string name, params IReadOnlyList<EventParameter> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Parameters = [.. parameters];
    }

    /// <summary>
    /// The event's name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The event's parameters, in the order a handler declares them.
    /// </summary>
    public IReadOnlyList<EventParameter> Parameters { get; }

    /// <summary>
    /// The event's name.
    /// </summary>
    /// <returns>The event's name.</returns>
    public override string ToString() => Name;
}

/// <summary>
/// A parameter of an event.
/// </summary>
/// <param name="Name">The name a generated handler gives the parameter, such as <c>KeyAscii</c>.</param>
/// <param name="TypeName">The declared type of the parameter, as VB names it, such as <c>Integer</c> or <c>MSForms.ReturnInteger</c>.</param>
public sealed record EventParameter(string Name, string TypeName);

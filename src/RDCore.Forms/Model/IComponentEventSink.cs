namespace RDCore.Forms.Model;

/// <summary>
/// Receives the events a running form's components raise, for the code that handles them.
/// </summary>
/// <remarks>
/// A control raises an event to the sink with the component that raised it, the event, and its arguments in the order of
/// <see cref="EventDefinition.Parameters"/>. A control array's <c>Index</c> is not among them: <see cref="ComponentInstance.Index"/>
/// gives it. Which procedures handle the event, and in which order, is the sink's to decide.
/// </remarks>
public interface IComponentEventSink
{
    /// <summary>
    /// Raises an event of a component, and returns once its handlers have run.
    /// </summary>
    /// <param name="source">The component that raised the event.</param>
    /// <param name="definition">The event raised.</param>
    /// <param name="arguments">
    /// The event's arguments. A handler's assignment to an argument is written back into this array, which is how a control reads
    /// what a handler made of a <c>KeyAscii</c> or a <c>Cancel</c>.
    /// </param>
    void Raise(ComponentInstance source, EventDefinition definition, FormValue[] arguments);
}

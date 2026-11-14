namespace RDCore.Forms.Drawing;

/// <summary>
/// An item of the output list of VB's <c>Print</c> method: a text, <c>Spc(n)</c>, or <c>Tab(n)</c>. A comma between items is a
/// <see cref="Tab"/> with no column.
/// </summary>
public abstract record PrintItem
{
    private PrintItem()
    {
    }

    /// <summary>
    /// A text, printed where the current point is. A carriage return, a line feed, or both together start a new line within it.
    /// </summary>
    /// <param name="Value">The text, as the runtime formats it: a number's leading and trailing spaces are part of it.</param>
    public sealed record Text(string Value) : PrintItem;

    /// <summary>
    /// <c>Spc(n)</c>: moves the current point on by a number of columns.
    /// </summary>
    /// <param name="Count">The number of columns; one below 0 counts as 0.</param>
    public sealed record Spc(int Count) : PrintItem;

    /// <summary>
    /// <c>Tab(n)</c>, <c>Tab</c> with no column, or a comma: moves the current point to a column, or to the next print zone.
    /// </summary>
    /// <param name="Column">
    /// The column, from 1, or <see langword="null"/> for the next print zone. One below 1 counts as 1.
    /// </param>
    public sealed record Tab(int? Column = null) : PrintItem;
}

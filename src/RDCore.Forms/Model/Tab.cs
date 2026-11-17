namespace RDCore.Forms.Model;

/// <summary>
/// One tab of a TabStrip: an item of its <see cref="Properties.Tabs"/>, which a program reaches through <see cref="Tabs"/>. A tab is
/// data, not a control: it holds no controls, and the TabStrip raises its events, with the tab's index.
/// </summary>
/// <remarks>
/// A tab's access key is a <c>&amp;</c> in its <see cref="Caption"/>, as a control's is.
/// </remarks>
/// <param name="Name">The tab's name, which <see cref="Tabs"/> finds it by.</param>
/// <param name="Caption">The text the tab shows.</param>
public sealed record Tab(string Name, string Caption)
{
    /// <summary>
    /// The text shown while the pointer rests on the tab; none where it is empty.
    /// </summary>
    public string ToolTipText { get; init; } = "";

    /// <summary>
    /// Whether the tab can be selected. A disabled tab is shown dimmed, and a click on it selects nothing.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Whether the tab is shown.
    /// </summary>
    public bool Visible { get; init; } = true;

    /// <summary>
    /// What a program keeps on the tab for its own use.
    /// </summary>
    public string Tag { get; init; } = "";
}

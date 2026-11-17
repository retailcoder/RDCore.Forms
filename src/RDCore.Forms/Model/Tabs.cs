using System.Collections;

namespace RDCore.Forms.Model;

/// <summary>
/// A TabStrip's tabs, in their order, as a program reaches them: its <c>Tabs</c>. They are the TabStrip's
/// <see cref="Properties.Tabs"/>, which every change here sets anew, so that its view and its <c>PropertyChanged</c> follow.
/// </summary>
/// <remarks>
/// The TabStrip's <c>Value</c> follows its selected tab as tabs are added and removed, as a MultiPage's follows its page
/// (<see cref="Pages"/>).
/// </remarks>
public sealed class Tabs : IReadOnlyList<Tab>
{
    private Tabs(ComponentInstance tabStrip) => TabStrip = tabStrip;

    /// <summary>
    /// The tabs of a TabStrip.
    /// </summary>
    /// <param name="tabStrip">The TabStrip.</param>
    /// <returns>Its tabs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tabStrip"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tabStrip"/> is not a TabStrip.</exception>
    public static Tabs Of(ComponentInstance tabStrip)
    {
        ArgumentNullException.ThrowIfNull(tabStrip);
        if (!ReferenceEquals(tabStrip.Class, ComponentClasses.TabStrip))
            throw new ArgumentException($"{tabStrip.Name} is not a TabStrip.", nameof(tabStrip));
        return new Tabs(tabStrip);
    }

    /// <summary>
    /// The TabStrip whose tabs these are.
    /// </summary>
    public ComponentInstance TabStrip { get; }

    private IReadOnlyList<Tab> All => TabStrip.Get(Properties.Tabs);

    /// <summary>
    /// How many tabs the TabStrip has.
    /// </summary>
    public int Count => All.Count;

    /// <summary>
    /// The tab at an index, from 0.
    /// </summary>
    /// <param name="index">The tab's index.</param>
    /// <returns>The tab.</returns>
    /// <exception cref="FormErrorException">No tab has the index: 5, Invalid procedure call or argument.</exception>
    public Tab this[int index] => index >= 0 && index < Count ? All[index] : throw new FormErrorException(FormError.InvalidProcedureCall);

    /// <summary>
    /// The tab of a name, compared without regard to case.
    /// </summary>
    /// <param name="name">The tab's name.</param>
    /// <returns>The tab.</returns>
    /// <exception cref="FormErrorException">No tab has the name: 5, Invalid procedure call or argument.</exception>
    public Tab this[string name] => this[IndexOf(name)];

    /// <summary>
    /// The selected tab, or <see langword="null"/> when the TabStrip's <c>Value</c> selects none: the TabStrip's <c>SelectedItem</c>.
    /// </summary>
    public Tab? SelectedItem => TabStrip.Get(Properties.SelectedIndex) is var selected && selected >= 0 && selected < Count ? All[selected] : null;

    /// <summary>
    /// The index of the tab of a name, compared without regard to case.
    /// </summary>
    /// <param name="name">The tab's name.</param>
    /// <returns>Its index, or -1 when no tab has the name.</returns>
    public int IndexOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        for (var index = 0; index < Count; index++)
        {
            if (string.Equals(All[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        return -1;
    }

    /// <summary>
    /// Adds a tab, as the TabStrip's <c>Tabs.Add</c> does.
    /// </summary>
    /// <param name="name">The tab's name; <see langword="null"/> names it <c>Tab</c> and the first number no tab has.</param>
    /// <param name="caption">The tab's caption; <see langword="null"/> captions it with its name.</param>
    /// <param name="index">Where among the tabs to add it, from 0; <see langword="null"/> adds it last.</param>
    /// <returns>The tab added.</returns>
    /// <exception cref="FormErrorException">
    /// <paramref name="index"/> is below 0 or past the last tab's, or another tab has the name, compared without regard to case: 5,
    /// Invalid procedure call or argument.
    /// </exception>
    public Tab Add(string? name = null, string? caption = null, int? index = null)
    {
        var at = index ?? Count;
        if (at < 0 || at > Count || (name is not null && IndexOf(name) >= 0))
            throw new FormErrorException(FormError.InvalidProcedureCall);
        name ??= FreeName();
        var tab = new Tab(name, caption ?? name);
        var tabs = All.ToList();
        tabs.Insert(at, tab);
        TabStrip.Set(Properties.Tabs, tabs);
        SelectedIndexes.Inserted(TabStrip, at, tabs.Count);
        return tab;
    }

    /// <summary>
    /// Puts a tab in the place of the one at an index: how a program's change to a tab's <c>Caption</c>, <c>Enabled</c> or another of
    /// its properties reaches the TabStrip.
    /// </summary>
    /// <param name="index">The tab's index.</param>
    /// <param name="tab">The tab to put there.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tab"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormErrorException">
    /// No tab has the index, or another tab has the new one's name, compared without regard to case: 5, Invalid procedure call or argument.
    /// </exception>
    public void Replace(int index, Tab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        _ = this[index];
        var named = IndexOf(tab.Name);
        if (named >= 0 && named != index)
            throw new FormErrorException(FormError.InvalidProcedureCall);
        var tabs = All.ToList();
        tabs[index] = tab;
        TabStrip.Set(Properties.Tabs, tabs);
    }

    /// <summary>
    /// Removes the tab at an index, as the TabStrip's <c>Tabs.Remove</c> does.
    /// </summary>
    /// <param name="index">The tab's index.</param>
    /// <exception cref="FormErrorException">No tab has the index: 5, Invalid procedure call or argument.</exception>
    public void Remove(int index)
    {
        _ = this[index];
        var tabs = All.ToList();
        tabs.RemoveAt(index);
        TabStrip.Set(Properties.Tabs, tabs);
        SelectedIndexes.Removed(TabStrip, index, tabs.Count);
    }

    /// <summary>
    /// Removes the tab of a name, compared without regard to case, as <see cref="Remove(int)"/> removes one at an index.
    /// </summary>
    /// <param name="name">The tab's name.</param>
    /// <exception cref="FormErrorException">No tab has the name: 5, Invalid procedure call or argument.</exception>
    public void Remove(string name) => Remove(IndexOf(name) is >= 0 and var index ? index : throw new FormErrorException(FormError.InvalidProcedureCall));

    /// <summary>
    /// Removes every tab, as the TabStrip's <c>Tabs.Clear</c> does; the TabStrip then has none selected.
    /// </summary>
    public void Clear()
    {
        TabStrip.Set(Properties.Tabs, []);
        TabStrip.Set(Properties.SelectedIndex, -1);
    }

    /// <inheritdoc/>
    public IEnumerator<Tab> GetEnumerator() => All.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private string FreeName()
    {
        var number = 1;
        while (IndexOf("Tab" + number.ToString(System.Globalization.CultureInfo.InvariantCulture)) >= 0)
            number++;
        return "Tab" + number.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

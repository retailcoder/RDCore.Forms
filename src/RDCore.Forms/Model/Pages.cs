using System.Collections;

namespace RDCore.Forms.Model;

/// <summary>
/// A MultiPage's pages, in their order, as a program reaches them: its <c>Pages</c>. A page is a <see cref="ComponentClasses.Page"/>
/// component, placed in the MultiPage, and its index is its place among the MultiPage's <see cref="ComponentInstance.Contents"/>.
/// </summary>
/// <remarks>
/// The MultiPage's <c>Value</c> (<see cref="Properties.SelectedIndex"/>) follows its selected page as pages are added and removed:
/// a page added before it, or one removed before it, moves it along; removing the selected page selects the one that takes its
/// place, or the last; a MultiPage with no pages has none selected (-1).
/// </remarks>
public sealed class Pages : IReadOnlyList<ComponentInstance>
{
    private readonly FormDefinition form;

    private Pages(FormDefinition form, ComponentInstance multiPage)
    {
        this.form = form;
        MultiPage = multiPage;
    }

    /// <summary>
    /// The pages of a MultiPage of a form.
    /// </summary>
    /// <param name="form">The form the MultiPage is on.</param>
    /// <param name="multiPage">The MultiPage.</param>
    /// <returns>Its pages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> or <paramref name="multiPage"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="multiPage"/> is not a MultiPage of <paramref name="form"/>.</exception>
    public static Pages Of(FormDefinition form, ComponentInstance multiPage)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(multiPage);
        if (!ReferenceEquals(multiPage.Class, ComponentClasses.MultiPage) || !form.Root.Descendants().Contains(multiPage))
            throw new ArgumentException($"{multiPage.Name} is not a MultiPage of {form.Name}.", nameof(multiPage));
        return new Pages(form, multiPage);
    }

    /// <summary>
    /// The MultiPage whose pages these are.
    /// </summary>
    public ComponentInstance MultiPage { get; }

    /// <summary>
    /// How many pages the MultiPage has.
    /// </summary>
    public int Count => MultiPage.Contents.Count;

    /// <summary>
    /// The page at an index, from 0.
    /// </summary>
    /// <param name="index">The page's index.</param>
    /// <returns>The page.</returns>
    /// <exception cref="FormErrorException">No page has the index: 5, Invalid procedure call or argument.</exception>
    public ComponentInstance this[int index] =>
        index >= 0 && index < Count ? MultiPage.Contents[index] : throw new FormErrorException(FormError.InvalidProcedureCall);

    /// <summary>
    /// The page of a name, compared without regard to case.
    /// </summary>
    /// <param name="name">The page's name.</param>
    /// <returns>The page.</returns>
    /// <exception cref="FormErrorException">No page has the name: 730, Control not found.</exception>
    public ComponentInstance this[string name] =>
        MultiPage.Contents.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new FormErrorException(FormError.ControlNotFound(name));

    /// <summary>
    /// The selected page, or <see langword="null"/> when the MultiPage's <c>Value</c> selects none: the MultiPage's <c>SelectedItem</c>.
    /// </summary>
    public ComponentInstance? SelectedItem =>
        MultiPage.Get(Properties.SelectedIndex) is var selected && selected >= 0 && selected < Count ? MultiPage.Contents[selected] : null;

    /// <summary>
    /// A page's index, from 0: its <c>Index</c>.
    /// </summary>
    /// <param name="page">The page.</param>
    /// <returns>Its index, or -1 when it is not one of these pages.</returns>
    public int IndexOf(ComponentInstance page)
    {
        for (var index = 0; index < Count; index++)
        {
            if (ReferenceEquals(MultiPage.Contents[index], page))
                return index;
        }

        return -1;
    }

    /// <summary>
    /// Adds a page, as the MultiPage's <c>Pages.Add</c> does, and raises the form's <see cref="FormDefinition.ControlAdded"/>.
    /// </summary>
    /// <param name="name">The page's name; <see langword="null"/> names it <c>Page</c> and the first number no component of the form has.</param>
    /// <param name="caption">The page's caption; <see langword="null"/> captions it with its name.</param>
    /// <param name="index">Where among the pages to add it, from 0; <see langword="null"/> adds it last.</param>
    /// <returns>The page added.</returns>
    /// <exception cref="FormErrorException">
    /// <paramref name="index"/> is below 0 or past the last page's: 5, Invalid procedure call or argument. Otherwise as
    /// <see cref="FormDefinition.AddControl(ComponentClass, string, ComponentInstance?)"/> for the name.
    /// </exception>
    public ComponentInstance Add(string? name = null, string? caption = null, int? index = null)
    {
        var at = index ?? Count;
        if (at < 0 || at > Count)
            throw new FormErrorException(FormError.InvalidProcedureCall);
        name ??= form.FreeName(ComponentClasses.Page.DefaultName);
        var page = form.AddControl(ComponentClasses.Page, name, MultiPage, at);
        page.Set(Properties.Caption, caption ?? name);
        SelectedIndexes.Inserted(MultiPage, at, Count);
        return page;
    }

    /// <summary>
    /// Removes the page at an index, with the controls on it, as the MultiPage's <c>Pages.Remove</c> does, and raises the form's
    /// <see cref="FormDefinition.ControlRemoved"/>. A page placed at design time is removed too.
    /// </summary>
    /// <param name="index">The page's index.</param>
    /// <exception cref="FormErrorException">No page has the index: 5, Invalid procedure call or argument.</exception>
    public void Remove(int index)
    {
        var page = this[index];
        form.RemovePage(page);
        SelectedIndexes.Removed(MultiPage, index, Count);
    }

    /// <summary>
    /// Removes the page of a name, compared without regard to case, as <see cref="Remove(int)"/> removes one at an index.
    /// </summary>
    /// <param name="name">The page's name.</param>
    /// <exception cref="FormErrorException">No page has the name: 730, Control not found.</exception>
    public void Remove(string name) => Remove(IndexOf(this[name]));

    /// <summary>
    /// Removes every page, as the MultiPage's <c>Pages.Clear</c> does; the MultiPage then has none selected.
    /// </summary>
    public void Clear()
    {
        while (Count > 0)
            Remove(Count - 1);
    }

    /// <inheritdoc/>
    public IEnumerator<ComponentInstance> GetEnumerator() => MultiPage.Contents.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// How a MultiPage's or a TabStrip's Value follows its selected page or tab as one is added or removed.
internal static class SelectedIndexes
{
    // One was added at index, so the collection holds count: the first one added is selected, and one added at or before the selected one moves it along.
    public static void Inserted(ComponentInstance component, int index, int count)
    {
        var selected = component.Get(Properties.SelectedIndex);
        if (count == 1 || selected < 0)
            component.Set(Properties.SelectedIndex, 0);
        else if (index <= selected && selected + 1 < count)
            component.Set(Properties.SelectedIndex, selected + 1);
    }

    // The one at index was removed, so the collection holds count: one before the selected one moves it back, and the selected one's place goes to
    // the one after it, or the last.
    public static void Removed(ComponentInstance component, int index, int count)
    {
        var selected = component.Get(Properties.SelectedIndex);
        if (count == 0)
            component.Set(Properties.SelectedIndex, -1);
        else if (index < selected)
            component.Set(Properties.SelectedIndex, selected - 1);
        else if (selected >= count)
            component.Set(Properties.SelectedIndex, count - 1);
    }
}

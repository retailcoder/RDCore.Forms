using System.Runtime.CompilerServices;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Model;

/// <summary>
/// The layout of a form's aligned PictureBoxes, as VB6 lays them out: each box on the form itself whose <c>Align</c> is set is docked
/// to that edge of the form's client area.
/// </summary>
/// <remarks>
/// <para>
/// A Data control and a placed UserControl that is <c>Alignable</c> are docked as a box is: a box here is any control whose class has
/// <c>Align</c> and a place of its own.
/// </para>
/// <para>
/// The boxes docked to the top and the bottom are laid out first, each across the form's whole width, then those docked to the left
/// and the right, each in the height the others leave. Each edge's boxes go from the edge inwards in the order of
/// <see cref="Docked"/>, and each takes its own height (or width) in whole pixels. A hidden box keeps its place and takes no room. A
/// box whose height leaves no room for the left and right boxes makes them 0 high; the bottom boxes go up past the top ones.
/// </para>
/// <para>
/// The layout follows the form's size, and each box's <c>Align</c>, <c>Visible</c>, <c>Width</c> and <c>Height</c>, as they
/// change, and the boxes a program adds and removes. A box whose <c>Align</c> is set to <see cref="Align.None"/> keeps its last place and size.
/// </para>
/// </remarks>
public sealed class AlignLayout
{
    private static readonly ConditionalWeakTable<FormDefinition, AlignLayout> layouts = new();

    private readonly List<ComponentInstance> docked = [];
    private bool laying;

    private AlignLayout(FormDefinition form)
    {
        Form = form;
        var root = form.Root;
        // A box in another container is not aligned, whatever its file says; a box of an MDI form is aligned to the top unless its
        // file says otherwise.
        foreach (var box in root.Descendants().Where(component => IsBox(component) && !ReferenceEquals(component.Container, root)))
        {
            if (box.Get(P.Align) != Align.None)
                box.Place(P.Align, Align.None);
        }
        // The file's boxes dock from the last one in, so the last one is the nearest its edge.
        foreach (var box in root.Contents.Where(IsBox).Reverse())
        {
            if (ReferenceEquals(root.Class, ComponentClasses.MDIForm) && box.Get(P.Align) == Align.None)
                box.Place(P.Align, Align.Top);
            if (box.Get(P.Align) != Align.None)
                docked.Add(box);
            box.PropertyChanged += OnBoxChanged;
        }
        root.PropertyChanged += OnRootChanged;
        form.ControlAdded += (_, e) => Added(e.Control);
        form.ControlRemoved += (_, e) => Removed(e.Control);
        LayOut();
    }

    /// <summary>
    /// The layout of <paramref name="form"/>'s aligned PictureBoxes, made and laid out the first time it is asked for.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <returns>The same layout for each call with the same form.</returns>
    public static AlignLayout Of(FormDefinition form)
    {
        ArgumentNullException.ThrowIfNull(form);
        return layouts.GetValue(form, made => new AlignLayout(made));
    }

    /// <summary>
    /// The form laid out.
    /// </summary>
    public FormDefinition Form { get; }

    /// <summary>
    /// The aligned boxes, in the order they dock: the file's from its last one, then each box as its <c>Align</c> is set or it is
    /// added. Of the boxes docked to one edge, the first is the nearest it.
    /// </summary>
    public IReadOnlyList<ComponentInstance> Docked => docked;

    /// <summary>
    /// Whether the layout follows the form's <c>Width</c> and <c>Height</c> as they change; <see langword="true"/> unless it is
    /// cleared. A window that raises its form's Resize before the boxes' clears it, and calls <see cref="LayOut"/> after the form's.
    /// </summary>
    public bool FollowsTheForm { get; set; } = true;

    /// <summary>
    /// Places each visible docked box on its edge.
    /// </summary>
    public void LayOut()
    {
        if (laying)
            return;
        laying = true;
        try
        {
            var root = Form.Root;
            var width = root.Get(P.Width);
            double top = 0, bottom = Pixels(root.Get(P.Height)), left = 0, right = Pixels(width);
            foreach (var box in Shown(Align.Top, Align.Bottom))
            {
                var height = Pixels(box.Get(P.Height));
                if (box.Get(P.Align) == Align.Top)
                {
                    Move(box, 0, top, width, box.Get(P.Height));
                    top += height;
                }
                else
                {
                    bottom -= height;
                    Move(box, 0, bottom, width, box.Get(P.Height));
                }
            }
            var between = Math.Max(0, bottom - top);
            foreach (var box in Shown(Align.Left, Align.Right))
            {
                var across = Pixels(box.Get(P.Width));
                if (box.Get(P.Align) == Align.Left)
                {
                    Move(box, left, top, box.Get(P.Width), between);
                    left += across;
                }
                else
                {
                    right -= across;
                    Move(box, right, top, box.Get(P.Width), between);
                }
            }
        }
        finally
        {
            laying = false;
        }
    }

    /// <summary>
    /// The error VB6 raises for a program's assignment of <paramref name="property"/> on <paramref name="component"/>, where its
    /// alignment refuses it: the <c>Left</c> and <c>Width</c> of a box docked to the top or the bottom, the <c>Top</c> and
    /// <c>Height</c> of one docked to the left or the right.
    /// </summary>
    /// <param name="component">The component assigned.</param>
    /// <param name="property">The property assigned.</param>
    /// <returns>Error 387, or <see langword="null"/> where the assignment is not refused for the alignment.</returns>
    /// <remarks>
    /// The <c>Top</c> of a box docked to the top or the bottom, and the <c>Left</c> of one docked to the left or the right, are not
    /// refused: an assignment of either leaves the box where it is.
    /// </remarks>
    public static FormError? Refusal(ComponentInstance component, PropertyDefinition property)
    {
        ArgumentNullException.ThrowIfNull(component);
        ArgumentNullException.ThrowIfNull(property);
        if (!IsDocked(component))
            return null;
        var refused = component.Get(P.Align) is Align.Top or Align.Bottom
            ? ReferenceEquals(property, P.Left) || ReferenceEquals(property, P.Width)
            : ReferenceEquals(property, P.Top) || ReferenceEquals(property, P.Height);
        return refused ? FormError.PropertyCantBeSetOnThisControl(property.Name) : null;
    }

    // What a docked box holds when a coordinate is set: its own, except its height for a top or bottom box and its width for a left
    // or right one.
    internal static double Kept(ComponentInstance component, PropertyDefinition<double> property, double value)
    {
        if (!IsDocked(component))
            return value;
        var free = component.Get(P.Align) is Align.Top or Align.Bottom ? P.Height : P.Width;
        return ReferenceEquals(property, free) ? value : component.Get(property);
    }

    // What a box holds when its Align is set: a box in another container, and a box of an MDI form set to None, keep theirs.
    internal static Align Taken(ComponentInstance component, Align value) => component.Container switch
    {
        null => value,
        { Class.Kind: not ComponentKind.Root } => component.Get(P.Align),
        { } root when ReferenceEquals(root.Class, ComponentClasses.MDIForm) && value == Align.None => component.Get(P.Align),
        _ => value,
    };

    // A control whose class has Align and a place of its own: a PictureBox, a Data control, or a placed UserControl that is Alignable
    // and not InvisibleAtRuntime.
    private static bool IsBox(ComponentInstance component) =>
        component.Class is { Kind: ComponentKind.Control, HasExtent: true } type && type.Has(P.Align);

    private static bool IsDocked(ComponentInstance component) =>
        IsBox(component) && component.Container is { Class.Kind: ComponentKind.Root } && component.Get(P.Align) != Align.None;

    // VB6 places a window in whole pixels.
    private static double Pixels(double value) => Math.Round(value, MidpointRounding.AwayFromZero);

    private IEnumerable<ComponentInstance> Shown(Align one, Align other) =>
        docked.Where(box => box.Get(P.Visible) && box.Get(P.Align) is var align && (align == one || align == other)).ToArray();

    private static void Move(ComponentInstance box, double left, double top, double width, double height)
    {
        Give(box, P.Left, left);
        Give(box, P.Top, top);
        Give(box, P.Width, width);
        Give(box, P.Height, height);
    }

    private static void Give(ComponentInstance box, PropertyDefinition<double> property, double value)
    {
        if (box.Get(property) != value)
            box.Place(property, value);
    }

    private void OnRootChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        if (FollowsTheForm && e.Property.Name is nameof(P.Width) or nameof(P.Height))
            LayOut();
    }

    private void OnBoxChanged(object? sender, ComponentPropertyChangedEventArgs e)
    {
        if (laying || sender is not ComponentInstance box)
            return;
        switch (e.Property.Name)
        {
            case nameof(P.Align):
                if (Equals(e.Before, e.After))
                    return;
                // A box aligned again docks inside the others.
                docked.Remove(box);
                if (IsDocked(box))
                    docked.Add(box);
                LayOut();
                break;
            case nameof(P.Visible) or nameof(P.Width) or nameof(P.Height) when docked.Contains(box):
                LayOut();
                break;
        }
    }

    private void Added(ComponentInstance control)
    {
        if (!IsBox(control) || !ReferenceEquals(control.Container, Form.Root))
            return;
        control.PropertyChanged += OnBoxChanged;
        if (IsDocked(control))
        {
            docked.Add(control);
            LayOut();
        }
    }

    private void Removed(ComponentInstance control)
    {
        control.PropertyChanged -= OnBoxChanged;
        if (docked.Remove(control))
            LayOut();
    }
}

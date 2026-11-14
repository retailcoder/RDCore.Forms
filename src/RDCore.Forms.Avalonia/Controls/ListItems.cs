using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Avalonia.Controls;

// A ListBox's or ComboBox's ItemData and ItemIcon, kept in step with its items as AddItem, RemoveItem and Clear change them. Each holds
// one value for each item; a value the component lacks is taken as 0, or as no icon.
internal static class ListItems
{
    // An item was added at the index, to a list of the count of items before it: its data is 0, and it has no icon.
    public static void Inserted(ComponentInstance instance, int count, int index)
    {
        instance.Set(P.ItemData, Spliced(instance.Get(P.ItemData), count, values => values.Insert(index, 0)));
        instance.Set(P.ItemIcon, Spliced(instance.Get(P.ItemIcon), count, values => values.Insert(index, null)));
    }

    // The item at the index was removed, from a list of the count of items before it.
    public static void Removed(ComponentInstance instance, int count, int index)
    {
        instance.Set(P.ItemData, Spliced(instance.Get(P.ItemData), count, values => values.RemoveAt(index)));
        instance.Set(P.ItemIcon, Spliced(instance.Get(P.ItemIcon), count, values => values.RemoveAt(index)));
    }

    public static void Cleared(ComponentInstance instance)
    {
        instance.Set(P.ItemData, []);
        instance.Set(P.ItemIcon, []);
    }

    public static int GetData(ComponentInstance instance, int count, int index)
    {
        Require(count, index);
        var values = instance.Get(P.ItemData);
        return index < values.Count ? values[index] : 0;
    }

    public static void SetData(ComponentInstance instance, int count, int index, int value)
    {
        Require(count, index);
        instance.Set(P.ItemData, Spliced(instance.Get(P.ItemData), count, values => values[index] = value));
    }

    public static byte[]? GetIcon(ComponentInstance instance, int count, int index)
    {
        Require(count, index);
        var values = instance.Get(P.ItemIcon);
        return index < values.Count ? values[index] : null;
    }

    // A picture the inspection refuses is refused here, with its error, so that an item never holds one. No bytes are no icon.
    public static void SetIcon(ComponentInstance instance, int count, int index, byte[]? picture)
    {
        Require(count, index);
        if (picture is { Length: 0 })
            picture = null;
        if (picture is not null && FormPictures.Inspect(picture, out _) is { } refused)
            throw new FormErrorException(refused);
        instance.Set(P.ItemIcon, Spliced(instance.Get(P.ItemIcon), count, values => values[index] = picture));
    }

    // A character typed in a ListBox: the next item after the selected one whose text starts with it, without regard to
    // case, from the top again past the last. None where no item does, and none where only the selected one does.
    public static int? NextStartingWith(System.Collections.IList items, int selected, char character)
    {
        var wanted = char.ToUpperInvariant(character);
        for (var step = 1; step <= items.Count; step++)
        {
            var index = (selected + step) % items.Count;
            if (items[index]?.ToString() is { Length: > 0 } text && char.ToUpperInvariant(text[0]) == wanted)
                return index == selected ? null : index;
        }
        return null;
    }

    private static void Require(int count, int index)
    {
        if (index < 0 || index >= count)
            throw new FormErrorException(FormError.InvalidPropertyArrayIndex);
    }

    // One value for each of the count of items, then the change.
    private static T[] Spliced<T>(IReadOnlyList<T> values, int count, Action<List<T>> change)
    {
        var list = new List<T>(count + 1);
        for (var i = 0; i < count; i++)
            list.Add(i < values.Count ? values[i] : default!);
        change(list);
        return [.. list];
    }
}

// A list's item icons, decoded once for each picture, and shown before the text of their items.
internal sealed class ItemIcons
{
    private Bitmap?[] pictures = [];
    private Dictionary<byte[], Bitmap?> decoded = new(ReferenceEqualityComparer.Instance);

    // Whether any item's icon is drawn. Then every item's text starts after an icon's square, so that the text lines up.
    public bool Any { get; private set; }

    public Bitmap? At(int index) => index >= 0 && index < pictures.Length ? pictures[index] : null;

    // Takes the component's ItemIcon. A picture the decoder refuses is not drawn.
    public void Take(IReadOnlyList<byte[]?> icons)
    {
        var kept = new Dictionary<byte[], Bitmap?>(ReferenceEqualityComparer.Instance);
        pictures = new Bitmap?[icons.Count];
        for (var i = 0; i < icons.Count; i++)
        {
            if (icons[i] is not { } bytes)
                continue;
            if (!kept.TryGetValue(bytes, out var picture))
            {
                if (!decoded.Remove(bytes, out picture))
                    picture = VBPicture.Decode(bytes, out _);
                kept[bytes] = picture;
            }
            pictures[i] = picture;
        }
        foreach (var stale in decoded.Values)
            stale?.Dispose();
        decoded = kept;
        Any = pictures.Any(picture => picture is not null);
    }

    // An item's container shows its text alone, or its icon then its text where any item has an icon.
    public void Show(Control container, object? item, int index)
    {
        if (container is ContentControl content)
            content.Content = Any ? new ItemView(item as string ?? "", At(index)) : item;
    }

    public void ShowAll(ItemsControl list)
    {
        foreach (var container in list.GetRealizedContainers())
        {
            var index = list.IndexFromContainer(container);
            if (index >= 0 && index < list.ItemCount)
                Show(container, list.Items[index], index);
        }
    }
}

// An item's icon, in a square as tall as its text and scaled to fit it, then its text.
internal sealed class ItemView : Control
{
    private const double Gap = 2;
    private readonly TextBlock text;
    private readonly Bitmap? icon;

    public ItemView(string text, Bitmap? icon)
    {
        this.text = new TextBlock { Text = text };
        this.icon = icon;
        LogicalChildren.Add(this.text);
        VisualChildren.Add(this.text);
    }

    private double Side => text.DesiredSize.Height;

    protected override Size MeasureOverride(Size availableSize)
    {
        text.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        return new Size(Side + Gap + text.DesiredSize.Width, Side);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        text.Arrange(new Rect(Side + Gap, 0, Math.Max(0, finalSize.Width - Side - Gap), finalSize.Height));
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        if (icon is null || Side <= 0)
            return;
        var pixels = icon.PixelSize;
        var scale = Math.Min(Side / pixels.Width, Side / pixels.Height);
        var (width, height) = (pixels.Width * scale, pixels.Height * scale);
        context.DrawImage(icon, new Rect(icon.Size), new Rect((Side - width) / 2, (Bounds.Height - height) / 2, width, height));
    }
}

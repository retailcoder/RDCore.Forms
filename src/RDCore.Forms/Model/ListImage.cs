namespace RDCore.Forms.Model;

/// <summary>
/// One picture of an ImageList: an item of its <see cref="Properties.ListImages"/>. Two are equal where their pictures have the same
/// bytes and their keys and tags are the same.
/// </summary>
/// <param name="Picture">The bytes of the picture's file: a bitmap or an icon.</param>
public sealed record ListImage(byte[] Picture)
{
    /// <summary>
    /// The text a program finds the picture by; none where it is empty.
    /// </summary>
    public string Key { get; init; } = "";

    /// <summary>
    /// What a program keeps on the picture for its own use.
    /// </summary>
    public string Tag { get; init; } = "";

    /// <inheritdoc/>
    public bool Equals(ListImage? other) =>
        other is not null && Key == other.Key && Tag == other.Tag && Picture.AsSpan().SequenceEqual(other.Picture);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Key, Tag, Picture.Length);
}

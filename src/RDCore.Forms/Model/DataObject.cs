namespace RDCore.Forms.Model;

/// <summary>
/// The data a drag carries, or a cut, copy or paste: <c>RDForms.DataObject</c>, the <c>Data</c> of a <c>BeforeDragOver</c> and a
/// <c>BeforeDropOrPaste</c>. It holds text in formats: the text format, and custom formats a program names.
/// </summary>
/// <remarks>
/// RDForms does not implement drag and drop, so no event gives a program one; a program can make one with <c>New</c>. Its
/// clipboard methods (<c>PutInClipboard</c>, <c>GetFromClipboard</c>) and <c>StartDrag</c> are not implemented.
/// </remarks>
public sealed class DataObject
{
    private readonly Dictionary<string, string> texts = new(StringComparer.Ordinal);

    // The key of the text format, which no custom format's name can be: a custom format's name is never empty.
    private const string TextFormat = "";

    /// <summary>
    /// The formats the object holds text in, the text format first where it holds one: <see langword="null"/> stands for the text
    /// format, and a string for a custom format of that name.
    /// </summary>
    public IReadOnlyList<string?> Formats =>
        [.. texts.Keys.OrderBy(key => key.Length == 0 ? 0 : 1).Select(key => key.Length == 0 ? null : key)];

    /// <summary>
    /// Takes out the text of every format.
    /// </summary>
    public void Clear() => texts.Clear();

    /// <summary>
    /// Whether the object holds text in a format.
    /// </summary>
    /// <param name="format">The name of a custom format, or <see langword="null"/> for the text format.</param>
    /// <returns><see langword="true"/> where it holds text in that format.</returns>
    /// <exception cref="ArgumentException"><paramref name="format"/> is empty.</exception>
    public bool GetFormat(string? format = null) => texts.ContainsKey(KeyOf(format));

    /// <summary>
    /// The text the object holds in a format.
    /// </summary>
    /// <param name="format">The name of a custom format, or <see langword="null"/> for the text format.</param>
    /// <returns>The text, or <see langword="null"/> where the object holds none in that format.</returns>
    /// <exception cref="ArgumentException"><paramref name="format"/> is empty.</exception>
    public string? GetText(string? format = null) => texts.GetValueOrDefault(KeyOf(format));

    /// <summary>
    /// Puts text in the object in a format, in place of the text it held in that format.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="format">The name of a custom format, or <see langword="null"/> for the text format.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="format"/> is empty.</exception>
    public void SetText(string text, string? format = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        texts[KeyOf(format)] = text;
    }

    private static string KeyOf(string? format)
    {
        if (format is null)
            return TextFormat;
        ArgumentException.ThrowIfNullOrEmpty(format);
        return format;
    }
}

/// <summary>
/// What a drop or a paste does with its data, which a handler can change: <c>RDForms.ReturnEffect</c>, the <c>Effect</c> of a
/// <c>BeforeDragOver</c> and a <c>BeforeDropOrPaste</c>.
/// </summary>
/// <param name="value">The effect the event proposes.</param>
public sealed class ReturnEffect(DropEffect value = DropEffect.None)
{
    /// <summary>
    /// The effect: the one the event proposed, or the one a handler set.
    /// </summary>
    public DropEffect Value { get; set; } = value;
}

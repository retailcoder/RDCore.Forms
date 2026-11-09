using System.Text;
using System.Xml;
using System.Xml.Linq;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Xaml;

/// <summary>
/// Reads and writes RDForms documents: a form as a constrained XAML dialect, with the classes of <see cref="VBClasses"/> as its
/// elements.
/// </summary>
/// <remarks>
/// <para>
/// The root element is the form's root component, such as a <c>Form</c>, in the <see cref="Namespace"/> namespace, with a
/// <c>FormatVersion</c> attribute. Each component is an element named for its class, with its <c>Name</c>, its <c>Index</c> in a
/// control array, and each property it sets as an attribute; a component placed in another is a child element of it. A font is a
/// property element, such as <c>Label.Font</c>, and so are a list's items, a root's control libraries, and the properties the model
/// does not have. Only the elements of the format are read: no other XAML element, markup extension or code-behind.
/// </para>
/// <para>
/// A read never throws on what a document holds. It keeps what the model cannot hold as text, as an <see cref="UnmodelledProperty"/>
/// or an <see cref="UnmodelledComponent"/>, and reports it in <see cref="FormXamlRead.Diagnostics"/>. A DTD is refused, nothing
/// outside the document is read, and a document is read up to <see cref="MaxDocumentCharacters"/> characters and
/// <see cref="MaxNestingDepth"/> components deep.
/// </para>
/// </remarks>
public static class FormXaml
{
    /// <summary>
    /// The XML namespace of the format's elements.
    /// </summary>
    public const string Namespace = "https://rubberduckvba.ca/schemas/rdforms";

    /// <summary>
    /// The format version this library writes, and the latest it reads: 1. A document of a later version is read anyway.
    /// </summary>
    public const int FormatVersion = 1;

    /// <summary>
    /// How deep the components of a document are read, the root's included: 2,500. A component nested deeper is kept as an
    /// <see cref="UnmodelledComponent"/>, its own contents included.
    /// </summary>
    public const int MaxNestingDepth = 2_500;

    /// <summary>
    /// The most characters a document is read to: 67,108,864. A larger document holds no form.
    /// </summary>
    public const int MaxDocumentCharacters = 64 * 1024 * 1024;

    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = MaxDocumentCharacters,
        CheckCharacters = false,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
    };

    /// <summary>
    /// Reads a document's text.
    /// </summary>
    /// <param name="document">The document's text.</param>
    /// <returns>The form, and what the read reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    public static FormXamlRead Read(string document)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var reader = XmlReader.Create(new StringReader(document), ReaderSettings);
        return ReadDocument(reader);
    }

    /// <summary>
    /// Reads a document from a file, its encoding as its XML declaration states, UTF-8 where it states none.
    /// </summary>
    /// <param name="path">The file's path.</param>
    /// <returns>The form, and what the read reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, or holds a NUL.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be accessed.</exception>
    public static FormXamlRead ReadFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, ReaderSettings);
        return ReadDocument(reader);
    }

    private static FormXamlRead ReadDocument(XmlReader reader)
    {
        var diagnostics = new FormXamlDiagnosticBag();
        XDocument document;
        try
        {
            document = XDocument.Load(reader, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
        }
        catch (XmlException e)
        {
            diagnostics.Add(FormXamlDiagnosticKind.MalformedDocument, e.LineNumber, e.Message);
            return new FormXamlRead(null, 0, diagnostics.Items);
        }

        var read = new FormXamlReader(diagnostics);
        var form = read.Read(document.Root!);
        return new FormXamlRead(form, read.Version, diagnostics.Items);
    }

    /// <summary>
    /// Writes a form as a document.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <returns>The document's text, indented, with no XML declaration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Only the properties a component sets are written; the others keep their class's default when the document is read. A list's
    /// <c>ItemData</c> and <c>ItemIcon</c> are written for its items, and a value past its last item is not written.
    /// </para>
    /// <para>
    /// A string that holds a character XML 1.0 does not allow, such as a NUL, is written with that character as a character
    /// reference, which <see cref="Read"/> reads back; another XML reader may refuse it.
    /// </para>
    /// </remarks>
    public static string Write(FormDefinition form)
    {
        ArgumentNullException.ThrowIfNull(form);
        var text = new StringBuilder();
        using (var writer = XmlWriter.Create(text, WriterSettings(omitDeclaration: true)))
            FormXamlWriter.Write(writer, form);
        return text.ToString();
    }

    /// <summary>
    /// Writes a form as a document in a file, encoded as UTF-8 with an XML declaration, replacing the file where it exists.
    /// </summary>
    /// <param name="form">The form.</param>
    /// <param name="path">The file's path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="form"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty, or holds a NUL.</exception>
    /// <exception cref="IOException">The file cannot be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The file cannot be accessed.</exception>
    /// <remarks>
    /// What is written is what <see cref="Write"/> writes.
    /// </remarks>
    public static void WriteFile(FormDefinition form, string path)
    {
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(path);
        using var stream = File.Create(path);
        using var writer = XmlWriter.Create(stream, WriterSettings(omitDeclaration: false));
        FormXamlWriter.Write(writer, form);
    }

    // New lines in attributes and text are written as character references, so that a reader's normalization keeps a CR and a tab.
    private static XmlWriterSettings WriterSettings(bool omitDeclaration) => new()
    {
        Indent = true,
        IndentChars = "  ",
        NewLineChars = "\n",
        NewLineHandling = NewLineHandling.Entitize,
        CheckCharacters = false,
        OmitXmlDeclaration = omitDeclaration,
        Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
    };
}

/// <summary>
/// What <see cref="FormXaml.Read"/> or <see cref="FormXaml.ReadFile"/> read from a document.
/// </summary>
public sealed class FormXamlRead
{
    internal FormXamlRead(FormDefinition? form, int formatVersion, IReadOnlyList<FormXamlDiagnostic> diagnostics)
    {
        Form = form;
        FormatVersion = formatVersion;
        Diagnostics = diagnostics;
    }

    /// <summary>
    /// The form, or <see langword="null"/> when the document holds none; <see cref="Diagnostics"/> then says why.
    /// </summary>
    public FormDefinition? Form { get; }

    /// <summary>
    /// The document's <c>FormatVersion</c>, or 0 where it has none that can be read.
    /// </summary>
    public int FormatVersion { get; }

    /// <summary>
    /// What the read reports, in the order of the document.
    /// </summary>
    public IReadOnlyList<FormXamlDiagnostic> Diagnostics { get; }
}

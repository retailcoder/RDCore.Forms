using System.Globalization;
using System.Resources;

namespace RDCore.Forms.Xaml;

/// <summary>
/// What a read of an RDForms document reports.
/// </summary>
public enum FormXamlDiagnosticKind
{
    /// <summary>
    /// The document is not well-formed XML, declares a DTD, or is larger than <see cref="FormXaml.MaxDocumentCharacters"/>, so it
    /// holds no form.
    /// </summary>
    MalformedDocument,
    /// <summary>
    /// The document's root element is not a Form, an MDIForm, a UserControl or a PropertyPage in the
    /// <see cref="FormXaml.Namespace"/> namespace, so it holds no form.
    /// </summary>
    NotAForm,
    /// <summary>
    /// The document's <c>FormatVersion</c> is later than <see cref="FormXaml.FormatVersion"/>. It is read anyway: what this version
    /// does not know is reported as it is met.
    /// </summary>
    NewerFormatVersion,
    /// <summary>
    /// A component has no <c>Name</c>, so it is kept as an <see cref="Model.UnmodelledComponent"/>. A root with no name leaves the
    /// document with no form.
    /// </summary>
    NamelessComponent,
    /// <summary>
    /// A component is nested deeper than <see cref="FormXaml.MaxNestingDepth"/> components, so it is kept as an
    /// <see cref="Model.UnmodelledComponent"/>, its own contents included.
    /// </summary>
    NestingTooDeep,
    /// <summary>
    /// An element is not a class of the model, nor an element the format has in that place, so it is kept as an
    /// <see cref="Model.UnmodelledComponent"/>.
    /// </summary>
    UnmodelledElement,
    /// <summary>
    /// A component is placed in one that cannot hold it, such as a control in a CommandButton, so it is placed in the nearest
    /// enclosing component that can.
    /// </summary>
    MisplacedComponent,
    /// <summary>
    /// An attribute or a property element names no property of the component's class, so it is kept as an
    /// <see cref="Model.UnmodelledProperty"/>.
    /// </summary>
    UnknownProperty,
    /// <summary>
    /// A property's value cannot be read as its type, or is outside its range, so it is kept as an
    /// <see cref="Model.UnmodelledProperty"/>.
    /// </summary>
    InvalidValue,
}

/// <summary>
/// How much a reported thing costs the read.
/// </summary>
public enum FormXamlSeverity
{
    /// <summary>
    /// Nothing is lost: the read notes what it met.
    /// </summary>
    Information,
    /// <summary>
    /// Part of the document is kept as text, or placed elsewhere than it was written.
    /// </summary>
    Warning,
    /// <summary>
    /// The document holds no form.
    /// </summary>
    Error,
}

/// <summary>
/// One thing a read of an RDForms document reports.
/// </summary>
/// <param name="Kind">What is reported.</param>
/// <param name="Severity">How much the reported thing costs the read.</param>
/// <param name="Line">The 1-based line of the document it is about, or 0 where the document has none, such as an empty one.</param>
/// <param name="Message">The report, in the current UI culture.</param>
public sealed record FormXamlDiagnostic(FormXamlDiagnosticKind Kind, FormXamlSeverity Severity, int Line, string Message)
{
    /// <summary>
    /// The line, the severity and the message.
    /// </summary>
    /// <returns>The diagnostic as text.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Line}: {Severity}: {Message}");
}

// The diagnostics of one read, each message taken from the resources in the current UI culture, in the order they were found: the
// document's order.
internal sealed class FormXamlDiagnosticBag
{
    private static readonly ResourceManager Messages = new("RDCore.Forms.Xaml.FormXamlMessages", typeof(FormXamlDiagnosticBag).Assembly);

    private readonly List<FormXamlDiagnostic> diagnostics = [];

    public IReadOnlyList<FormXamlDiagnostic> Items => diagnostics;

    public void Add(FormXamlDiagnosticKind kind, int line, params object?[] arguments) => Add(kind, SeverityOf(kind), line, arguments);

    public void Add(FormXamlDiagnosticKind kind, FormXamlSeverity severity, int line, params object?[] arguments)
    {
        var format = Messages.GetString(kind.ToString(), CultureInfo.CurrentUICulture) ?? kind.ToString();
        diagnostics.Add(new FormXamlDiagnostic(kind, severity, line, string.Format(CultureInfo.CurrentCulture, format, arguments)));
    }

    private static FormXamlSeverity SeverityOf(FormXamlDiagnosticKind kind) => kind switch
    {
        FormXamlDiagnosticKind.MalformedDocument or FormXamlDiagnosticKind.NotAForm => FormXamlSeverity.Error,
        FormXamlDiagnosticKind.NewerFormatVersion or FormXamlDiagnosticKind.UnmodelledElement => FormXamlSeverity.Information,
        _ => FormXamlSeverity.Warning,
    };
}

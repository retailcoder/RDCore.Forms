using System.Globalization;
using System.Resources;

namespace RDCore.Forms.Import.VB6;

/// <summary>
/// What a VB6 import reports about a line of a designer file.
/// </summary>
public enum VB6ImportDiagnosticKind
{
    /// <summary>
    /// The file has no Begin block, so it holds no form.
    /// </summary>
    NoDesignerBlock,

    /// <summary>
    /// The file's root block is not a Form, an MDIForm, a UserControl or a PropertyPage, so the block is kept as text.
    /// </summary>
    NotAFormRoot,

    /// <summary>
    /// A line of the designer half is neither a block's Begin or End, nor a property.
    /// </summary>
    MalformedLine,

    /// <summary>
    /// A Begin or BeginProperty has no matching End or EndProperty. A BeginProperty left open ends at the End of its block, and a
    /// Begin left open ends with the file.
    /// </summary>
    UnbalancedBlock,

    /// <summary>
    /// An EndProperty closes no open BeginProperty, so the line is skipped.
    /// </summary>
    UnmatchedEnd,

    /// <summary>
    /// A Begin line names no component, so the block is kept as text. A root with no name leaves the file with no form.
    /// </summary>
    NamelessComponent,

    /// <summary>
    /// A block is nested deeper than <see cref="VB6FormReader.MaxNestingDepth"/> blocks, so it is kept as text, its own contents
    /// included.
    /// </summary>
    NestingTooDeep,

    /// <summary>
    /// A header line names a control library by something other than its type library's id, such as a project of the same group, so
    /// it is kept as text.
    /// </summary>
    UnreadLibraryReference,

    /// <summary>
    /// A header line is not a control library's, so it is kept as text.
    /// </summary>
    UnmodelledHeaderLine,

    /// <summary>
    /// A component's class is not one the model has as a control or a menu, so the component is kept as text.
    /// </summary>
    UnmodelledClass,

    /// <summary>
    /// A component is inside a component that cannot hold it, such as a control inside a ListBox, so it is placed in the nearest
    /// enclosing component that can, with its position as written.
    /// </summary>
    MisplacedComponent,

    /// <summary>
    /// A property's value is not one the property can hold, or the property cites a companion record and is of a type that has none,
    /// so the property is kept as an <see cref="Model.UnmodelledProperty"/>. An <c>Index</c> that is not a number from 0 to 32767 is
    /// kept the same way, and the component is in no control array.
    /// </summary>
    InvalidValue,

    /// <summary>
    /// A property cites a companion binary file that was not found, or by a name that is not looked for (a rooted path, or a name
    /// that holds a NUL), so the property is kept as text.
    /// </summary>
    MissingCompanion,

    /// <summary>
    /// A property cites a record the companion binary file does not hold, or holds in a shape the property cannot read, so the
    /// property is kept as text.
    /// </summary>
    MissingRecord,
}

/// <summary>
/// How much a <see cref="VB6ImportDiagnostic"/> costs the import.
/// </summary>
public enum VB6ImportSeverity
{
    /// <summary>
    /// The import kept something it does not model as text.
    /// </summary>
    Information,

    /// <summary>
    /// The import left out, kept as text, or placed elsewhere than written something the file sets, or could not read a line.
    /// </summary>
    Warning,

    /// <summary>
    /// The file holds no form the import can read.
    /// </summary>
    Error,
}

/// <summary>
/// One thing a VB6 import reports about the file it read.
/// </summary>
/// <param name="Kind">What is reported.</param>
/// <param name="Severity">How much the reported thing costs the import.</param>
/// <param name="Line">The 1-based line of the designer file it is about.</param>
/// <param name="Message">The report, in the current UI culture.</param>
public sealed record VB6ImportDiagnostic(VB6ImportDiagnosticKind Kind, VB6ImportSeverity Severity, int Line, string Message)
{
    /// <summary>
    /// The line, the severity and the message.
    /// </summary>
    /// <returns>The diagnostic as text.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Line}: {Severity}: {Message}");
}

// The diagnostics of one import, each message taken from the resources in the current UI culture.
internal sealed class DiagnosticBag
{
    private static readonly ResourceManager Messages =
        new("RDCore.Forms.Import.VB6.VB6ImportMessages", typeof(DiagnosticBag).Assembly);

    private readonly List<VB6ImportDiagnostic> diagnostics = [];

    // In the order of the lines they are about; diagnostics about the same line in the order they were reported.
    public IReadOnlyList<VB6ImportDiagnostic> Items => [.. diagnostics.OrderBy(d => d.Line)];

    public void Add(VB6ImportDiagnosticKind kind, int line, params object?[] arguments) =>
        Add(kind, SeverityOf(kind), line, arguments);

    public void Add(VB6ImportDiagnosticKind kind, VB6ImportSeverity severity, int line, params object?[] arguments)
    {
        var format = Messages.GetString(kind.ToString(), CultureInfo.CurrentUICulture) ?? kind.ToString();
        diagnostics.Add(new VB6ImportDiagnostic(kind, severity, line, string.Format(CultureInfo.CurrentCulture, format, arguments)));
    }

    private static VB6ImportSeverity SeverityOf(VB6ImportDiagnosticKind kind) => kind switch
    {
        VB6ImportDiagnosticKind.NoDesignerBlock or VB6ImportDiagnosticKind.NotAFormRoot => VB6ImportSeverity.Error,
        VB6ImportDiagnosticKind.UnmodelledClass or VB6ImportDiagnosticKind.UnreadLibraryReference
            or VB6ImportDiagnosticKind.UnmodelledHeaderLine => VB6ImportSeverity.Information,
        _ => VB6ImportSeverity.Warning,
    };
}

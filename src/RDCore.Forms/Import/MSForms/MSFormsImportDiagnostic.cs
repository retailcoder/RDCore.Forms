using System.Globalization;
using System.Resources;
using RDCore.Forms.Porting;

namespace RDCore.Forms.Import.MSForms;

/// <summary>
/// What an MSForms import reports about a UserForm or one of its controls.
/// </summary>
public enum MSFormsImportDiagnosticKind
{
    /// <summary>
    /// The file has no Begin block, so it holds no form.
    /// </summary>
    NoDesignerBlock,

    /// <summary>
    /// The file's root block is not a VBA UserForm's, or names no form, so the file holds no form the reader imports.
    /// </summary>
    NotAUserForm,

    /// <summary>
    /// A line of the designer block cannot be read, so it is left out.
    /// </summary>
    MalformedLine,

    /// <summary>
    /// The form cites no designer data, or its companion file was not found or holds none where the form says, so the form is
    /// imported from its designer block alone, with no controls.
    /// </summary>
    MissingCompanion,

    /// <summary>
    /// The form's designer data cannot be read, so the form is imported from its designer block alone, with no controls.
    /// </summary>
    UnreadableDesignerData,

    /// <summary>
    /// A control is of a class RDForms does not have, such as an ActiveX control, so it is kept as an
    /// <see cref="Model.UnmodelledComponent"/> and the controls it holds are left out.
    /// </summary>
    UnmodelledClass,

    /// <summary>
    /// A property's value is not one its RDForms property can hold, so the property is kept as an <see cref="Model.UnmodelledProperty"/>.
    /// </summary>
    InvalidValue,

    /// <summary>
    /// A property's value has no equivalent in RDForms, such as a raised <c>SpecialEffect</c>, so the property is kept as an
    /// <see cref="Model.UnmodelledProperty"/>.
    /// </summary>
    ValueNotPorted,

    /// <summary>
    /// A property sets something RDForms does not implement, such as a <c>Zoom</c> other than 100, so the property is kept as an
    /// <see cref="Model.UnmodelledProperty"/>.
    /// </summary>
    NotImplemented,

    /// <summary>
    /// A picture is a metafile. It is kept in the model, and RDForms does not draw it.
    /// </summary>
    MetafilePicture,

    /// <summary>
    /// An <c>Accelerator</c> names a key its control's caption does not hold, so the caption has no access key.
    /// </summary>
    AcceleratorNotInCaption,

    /// <summary>
    /// A <c>ControlSource</c> or <c>RowSource</c> is kept as text, and nothing resolves it. <see cref="MSFormsImportDiagnostic.Port"/> is
    /// the port diagnostic of the same fact.
    /// </summary>
    UnresolvedDataSource,
}

/// <summary>
/// How much an <see cref="MSFormsImportDiagnostic"/> costs the import.
/// </summary>
public enum MSFormsImportSeverity
{
    /// <summary>
    /// The import kept something RDForms does not have.
    /// </summary>
    Information,

    /// <summary>
    /// The import left out or changed something the form sets, or kept it in a shape RDForms does not use.
    /// </summary>
    Warning,

    /// <summary>
    /// The file holds no form the import can read.
    /// </summary>
    Error,
}

/// <summary>
/// One thing an MSForms import reports about the form it read.
/// </summary>
/// <param name="Kind">What is reported.</param>
/// <param name="Severity">How much the reported thing costs the import.</param>
/// <param name="Component">The name of the form or the control it is about.</param>
/// <param name="Property">The name of the MSForms property it is about, or <see langword="null"/> when it is about the whole component.</param>
/// <param name="Message">The report, in the current UI culture.</param>
public sealed record MSFormsImportDiagnostic(MSFormsImportDiagnosticKind Kind, MSFormsImportSeverity Severity, string Component, string? Property,
    string Message)
{
    /// <summary>
    /// The port diagnostic of the same fact, such as <see cref="PortDiagnosticKind.UnresolvedDataSource"/> with its code, or
    /// <see langword="null"/> when there is none.
    /// </summary>
    public PortDiagnostic? Port { get; init; }

    /// <summary>
    /// The component, the severity and the message.
    /// </summary>
    /// <returns>The diagnostic as text.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Component}: {Severity}: {Message}");
}

// The diagnostics of one import, each message taken from the resources in the current UI culture.
internal sealed class MSFormsDiagnosticBag
{
    private static readonly ResourceManager Messages =
        new("RDCore.Forms.Import.MSForms.MSFormsImportMessages", typeof(MSFormsDiagnosticBag).Assembly);

    private readonly List<MSFormsImportDiagnostic> diagnostics = [];

    // In the order they were reported: the form's own, then its controls' in the order of their sites, depth first.
    public IReadOnlyList<MSFormsImportDiagnostic> Items => [.. diagnostics];

    public void Add(MSFormsImportDiagnosticKind kind, string component, string? property, params object?[] arguments) =>
        AddMessage(kind, kind.ToString(), component, property, null, arguments);

    // A kind with more than one message, or with the port diagnostic of the same fact.
    public void AddMessage(MSFormsImportDiagnosticKind kind, string key, string component, string? property, PortDiagnostic? port,
        params object?[] arguments)
    {
        var format = Messages.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        var message = string.Format(CultureInfo.CurrentCulture, format, arguments);
        diagnostics.Add(new MSFormsImportDiagnostic(kind, SeverityOf(kind), component, property, message) { Port = port });
    }

    private static MSFormsImportSeverity SeverityOf(MSFormsImportDiagnosticKind kind) => kind switch
    {
        MSFormsImportDiagnosticKind.NoDesignerBlock or MSFormsImportDiagnosticKind.NotAUserForm => MSFormsImportSeverity.Error,
        MSFormsImportDiagnosticKind.UnmodelledClass => MSFormsImportSeverity.Information,
        _ => MSFormsImportSeverity.Warning,
    };
}

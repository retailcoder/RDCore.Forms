using static RDCore.Forms.Porting.PortDiagnosticSeverity;

namespace RDCore.Forms.Porting;

// The code and the severity of every port diagnostic: the one table of the RDC40000-40999 family. A code, once shipped, stays with its
// kind; a new kind takes the next code.
internal static class PortDiagnosticCodes
{
    private static readonly Dictionary<PortDiagnosticKind, (string Code, PortDiagnosticSeverity Severity)> Table = new()
    {
        [PortDiagnosticKind.AcceleratorAssignment] = ("RDC40001", Error),
        [PortDiagnosticKind.ValueNotConvertible] = ("RDC40002", Error),
        [PortDiagnosticKind.ExpressionNotConvertible] = ("RDC40003", Error),
        [PortDiagnosticKind.ComputedProgId] = ("RDC40004", Error),
        [PortDiagnosticKind.UnknownProgId] = ("RDC40005", Error),
        [PortDiagnosticKind.LateBoundMember] = ("RDC40006", Error),
        [PortDiagnosticKind.UnresolvedDataSource] = ("RDC40007", Warning),
        [PortDiagnosticKind.ExitPortedToValidate] = ("RDC40008", Warning),
        [PortDiagnosticKind.ScrollBarReference] = ("RDC40009", Warning),
        [PortDiagnosticKind.UnitWrapped] = ("RDC40010", Hint),
        [PortDiagnosticKind.CoordinatesConverted] = ("RDC40011", Hint),
        [PortDiagnosticKind.ScrollBarTypeNameChanged] = ("RDC40012", Warning),
        [PortDiagnosticKind.FindWindowThunderDFrame] = ("RDC40013", Warning),
    };

    public static string CodeOf(PortDiagnosticKind kind) => Table[kind].Code;

    public static PortDiagnosticSeverity SeverityOf(PortDiagnosticKind kind) => Table[kind].Severity;
}

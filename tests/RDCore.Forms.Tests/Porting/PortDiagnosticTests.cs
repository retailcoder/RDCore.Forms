using System.Globalization;
using RDCore.Forms.Porting;

namespace RDCore.Forms.Tests.Porting;

[TestClass]
public sealed class PortDiagnosticTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-CA");
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-CA");

    private static PortDiagnostic Of(PortDiagnosticKind kind) => kind switch
    {
        PortDiagnosticKind.AcceleratorAssignment => PortDiagnostic.AcceleratorAssignment(),
        PortDiagnosticKind.ValueNotConvertible => PortDiagnostic.ValueNotConvertible(4),
        PortDiagnosticKind.ExpressionNotConvertible => PortDiagnostic.ExpressionNotConvertible("TextAlign", "Alignment"),
        PortDiagnosticKind.ComputedProgId => PortDiagnostic.ComputedProgId(),
        PortDiagnosticKind.UnknownProgId => PortDiagnostic.UnknownProgId("Forms.HTMLText.1"),
        PortDiagnosticKind.LateBoundMember => PortDiagnostic.LateBoundMember("ControlTipText"),
        PortDiagnosticKind.UnresolvedDataSource => PortDiagnostic.UnresolvedDataSource("Sheet1!A1"),
        PortDiagnosticKind.ExitPortedToValidate => PortDiagnostic.ExitPortedToValidate(),
        PortDiagnosticKind.ScrollBarReference => PortDiagnostic.ScrollBarReference(),
        PortDiagnosticKind.UnitWrapped => PortDiagnostic.UnitWrapped("FromTwips"),
        PortDiagnosticKind.CoordinatesConverted => PortDiagnostic.CoordinatesConverted("ToPoints"),
        PortDiagnosticKind.ScrollBarTypeNameChanged => PortDiagnostic.ScrollBarTypeNameChanged("RDForms.HScrollBar"),
        PortDiagnosticKind.FindWindowThunderDFrame => PortDiagnostic.FindWindowThunderDFrame("FindWindowA"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    private static T InCulture<T>(CultureInfo culture, Func<T> read)
    {
        var before = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
        try
        {
            return read();
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }

    [TestMethod]
    public void EveryKind_HasTheNextCodeOfTheFamily_InTheOrderOfTheKinds()
    {
        var kinds = Enum.GetValues<PortDiagnosticKind>();
        for (var i = 0; i < kinds.Length; i++)
        {
            var expected = "RDC" + (40001 + i).ToString(CultureInfo.InvariantCulture);
            Assert.AreEqual(expected, PortDiagnostic.CodeOf(kinds[i]), kinds[i].ToString());
            Assert.AreEqual(expected, Of(kinds[i]).Code);
            Assert.AreEqual(kinds[i], Of(kinds[i]).Kind);
        }
    }

    [TestMethod]
    [DataRow(PortDiagnosticKind.AcceleratorAssignment, PortDiagnosticSeverity.Error)]
    [DataRow(PortDiagnosticKind.ValueNotConvertible, PortDiagnosticSeverity.Error)]
    [DataRow(PortDiagnosticKind.ExpressionNotConvertible, PortDiagnosticSeverity.Error)]
    [DataRow(PortDiagnosticKind.ComputedProgId, PortDiagnosticSeverity.Error)]
    [DataRow(PortDiagnosticKind.UnknownProgId, PortDiagnosticSeverity.Error)]
    [DataRow(PortDiagnosticKind.LateBoundMember, PortDiagnosticSeverity.Error)]
    [DataRow(PortDiagnosticKind.UnresolvedDataSource, PortDiagnosticSeverity.Warning)]
    [DataRow(PortDiagnosticKind.ExitPortedToValidate, PortDiagnosticSeverity.Warning)]
    [DataRow(PortDiagnosticKind.ScrollBarReference, PortDiagnosticSeverity.Warning)]
    [DataRow(PortDiagnosticKind.UnitWrapped, PortDiagnosticSeverity.Hint)]
    [DataRow(PortDiagnosticKind.CoordinatesConverted, PortDiagnosticSeverity.Hint)]
    [DataRow(PortDiagnosticKind.ScrollBarTypeNameChanged, PortDiagnosticSeverity.Warning)]
    [DataRow(PortDiagnosticKind.FindWindowThunderDFrame, PortDiagnosticSeverity.Warning)]
    public void EveryKind_HasItsSeverity(PortDiagnosticKind kind, PortDiagnosticSeverity severity)
    {
        Assert.AreEqual(severity, PortDiagnostic.SeverityOf(kind));
        Assert.AreEqual(severity, Of(kind).Severity);
    }

    [TestMethod]
    public void EveryKind_HasAnEnglishAndAFrenchMessage_WithItsArguments()
    {
        foreach (var kind in Enum.GetValues<PortDiagnosticKind>())
        {
            var diagnostic = Of(kind);
            var english = diagnostic.MessageIn(English);
            var french = diagnostic.MessageIn(French);
            Assert.AreNotEqual(kind.ToString(), english);
            Assert.AreNotEqual(english, french, kind.ToString());
            foreach (var argument in diagnostic.Arguments)
            {
                Assert.Contains(argument, english);
                Assert.Contains(argument, french);
            }

            Assert.DoesNotContain(" :", english);
            Assert.DoesNotContain("{", english);
            Assert.DoesNotContain("{", french);
            // Québec typography: a space before a colon, and none before a semicolon, an exclamation or a question mark.
            for (var i = french.IndexOf(':'); i >= 0; i = french.IndexOf(':', i + 1))
            {
                Assert.AreEqual(' ', french[i - 1], french);
            }

            Assert.DoesNotContain(" ;", french);
            Assert.DoesNotContain(" !", french);
            Assert.DoesNotContain(" ?", french);
        }
    }

    [TestMethod]
    public void AMessage_IsReadInTheCurrentUICulture()
    {
        var diagnostic = PortDiagnostic.ValueNotConvertible(4);
        Assert.AreEqual("4 has no RDForms value, so the port leaves it as written.", InCulture(English, () => diagnostic.Message));
        Assert.AreEqual("4 n'a pas de valeur RDForms, donc le portage le laisse tel quel.", InCulture(French, () => diagnostic.Message));
        Assert.AreEqual("RDC40002: 4 has no RDForms value, so the port leaves it as written.", InCulture(English, diagnostic.ToString));
    }

    [TestMethod]
    public void FindWindowThunderDFrame_IsRDC40013_AWarning_NamingTheFunctionAndTheClass()
    {
        var diagnostic = PortDiagnostic.FindWindowThunderDFrame("FindWindowA");
        Assert.AreEqual("RDC40013", diagnostic.Code);
        Assert.AreEqual(PortDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.AreEqual(
            "FindWindowA looks for a UserForm's window by its class, ThunderDFrame. A ported form is an RDForms window, so the call finds none: "
            + "to change the form's icon, set its Icon property.",
            diagnostic.MessageIn(English));
        Assert.AreEqual(
            "FindWindowA cherche la fenêtre d'un UserForm par sa classe, ThunderDFrame. Un formulaire porté est une fenêtre RDForms, donc l'appel "
            + "n'en trouve aucune : pour changer l'icône du formulaire, affectez sa propriété Icon.",
            diagnostic.MessageIn(French));
        Assert.Contains(FormsPortMap.MSForms.FormWindowClass!, diagnostic.MessageIn(English));
    }

    [TestMethod]
    public void TwoDiagnostics_AreEqual_WhenOfTheSameKindWithTheSameArguments()
    {
        Assert.AreEqual(PortDiagnostic.ValueNotConvertible(4), PortDiagnostic.ValueNotConvertible(4L));
        Assert.AreEqual(PortDiagnostic.ValueNotConvertible(4).GetHashCode(), PortDiagnostic.ValueNotConvertible(4L).GetHashCode());
        Assert.AreNotEqual(PortDiagnostic.ValueNotConvertible(4), PortDiagnostic.ValueNotConvertible(5));
        Assert.AreNotEqual(PortDiagnostic.UnitWrapped("FromTwips"), PortDiagnostic.CoordinatesConverted("FromTwips"));
        Assert.AreEqual(PortDiagnostic.ComputedProgId(), PortDiagnostic.ComputedProgId());
    }
}

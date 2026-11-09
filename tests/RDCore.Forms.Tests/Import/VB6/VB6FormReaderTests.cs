using System.Globalization;
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Import.VB6;

[TestClass]
public sealed class VB6FormReaderTests
{
    private static string Form(params string[] contents) =>
        string.Join("\r\n", [
            "VERSION 5.00",
            "Begin VB.Form Form1 ",
            "   Caption         =   \"Form1\"",
            "   ClientHeight    =   3000",
            "   ClientWidth     =   4000",
            .. contents,
            "End",
            "Attribute VB_Name = \"Form1\"",
            ""]);

    private static string[] Label(params string[] properties) =>
        ["   Begin VB.Label Label1 ", .. properties.Select(p => "      " + p), "   End"];

    private static byte[] Bytes(params int[] bytes) => bytes.Select(b => (byte)b).ToArray();

    [TestMethod]
    public void AFileWithNoBeginBlock_HoldsNoForm()
    {
        var import = VB6FormReader.Read("Attribute VB_Name = \"Module1\"\r\nOption Explicit\r\n");

        Assert.IsNull(import.Form);
        Assert.AreEqual(VB6ImportSeverity.Error, import.Diagnostics.Single(d => d.Kind == VB6ImportDiagnosticKind.NoDesignerBlock).Severity);
    }

    [TestMethod]
    public void ARootThatIsNoForm_HoldsNoForm()
    {
        var import = VB6FormReader.Read("VERSION 5.00\r\nBegin VB.CommandButton Command1 \r\nEnd\r\n");

        Assert.IsNull(import.Form);
        Assert.AreEqual(VB6ImportDiagnosticKind.NotAFormRoot, import.Diagnostics.Single().Kind);
        Assert.AreEqual(2, import.Diagnostics.Single().Line);
        Assert.AreEqual(new UnmodelledComponent("VB.CommandButton", "Command1", 0, "Begin VB.CommandButton Command1 \r\nEnd"), import.UnmodelledRoot);
    }

    [TestMethod]
    public void PropertyAndClassNames_AreReadWithoutRegardToCase()
    {
        var import = VB6FormReader.Read(Form("   begin vb.label Label1 ", "      CAPTION = \"x\"", "   End"));

        Assert.AreEqual("x", import.Form!.Find("Label1")!.Get(VBProperties.Caption));
        Assert.IsEmpty(import.Diagnostics);
    }

    [TestMethod]
    public void AValueThePropertyCannotHold_IsKeptAsText_AndReported()
    {
        var import = VB6FormReader.Read(Form(Label("Alignment       =   7")));
        var label = import.Form!.Find("Label1")!;

        Assert.IsFalse(label.IsSet(VBProperties.Alignment));
        Assert.Contains(new UnmodelledProperty("Alignment", "7"), label.UnmodelledProperties);
        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual((VB6ImportDiagnosticKind.InvalidValue, 7), (diagnostic.Kind, diagnostic.Line));
    }

    [TestMethod]
    public void APaletteColour_IsNoColourTheModelHolds_SoItIsKeptAsText()
    {
        var import = VB6FormReader.Read(Form(Label("BackColor       =   &H02C0C0C0&")));

        Assert.Contains(new UnmodelledProperty("BackColor", "&H02C0C0C0&"), import.Form!.Find("Label1")!.UnmodelledProperties);
        Assert.AreEqual(VB6ImportDiagnosticKind.InvalidValue, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void ACitationWithNoCompanion_IsKeptAsText_AndReported()
    {
        var import = VB6FormReader.Read(Form(Label("Caption         =   \"Form1.frx\":0000")));

        Assert.Contains(new UnmodelledProperty("Caption", "\"Form1.frx\":0000"), import.Form!.Find("Label1")!.UnmodelledProperties);
        Assert.AreEqual(VB6ImportDiagnosticKind.MissingCompanion, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    [DataRow("Caption         =   \"Form1.frx\":0010", DisplayName = "an offset past the end")]
    [DataRow("Caption         =   $\"Form1.frx\":0000", DisplayName = "a length past the end")]
    public void ACitationOfNoRecord_IsKeptAsText_AndReported(string caption)
    {
        var import = VB6FormReader.Read(Form(Label(caption)), Bytes(0x05, 0x00, 0x00, 0x00, (byte)'a'));

        Assert.IsFalse(import.Form!.Find("Label1")!.IsSet(VBProperties.Caption));
        Assert.AreEqual(VB6ImportDiagnosticKind.MissingRecord, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AShortStringRecord_IsReadUpToItsFirstNul()
    {
        // A 4-byte length cited as a short string: VB6 takes its first byte as the length and reads an empty caption.
        var import = VB6FormReader.Read(Form(Label("Caption         =   \"Form1.frx\":0000")), Bytes(0x02, 0x00, 0x00, 0x00, (byte)'a', (byte)'b'));

        Assert.AreEqual("", import.Form!.Find("Label1")!.Get(VBProperties.Caption));
        Assert.IsEmpty(import.Diagnostics);
    }

    [TestMethod]
    public void AStringRecord_IsDecodedWithTheOptionsCodePage()
    {
        var import = VB6FormReader.Read(Form(Label("Caption         =   \"Form1.frx\":0000")), Bytes(0x04, 0x63, 0x61, 0x66, 0xE9));

        Assert.AreEqual("café", import.Form!.Find("Label1")!.Get(VBProperties.Caption));
    }

    [TestMethod]
    public void AListCitedAsALongString_IsAValueTheListCannotHold()
    {
        var import = VB6FormReader.Read(Form("   Begin VB.ListBox List1 ", "      List            =   $\"Form1.frx\":0000", "   End"),
            Bytes(0x00, 0x00));

        Assert.AreEqual(VB6ImportDiagnosticKind.InvalidValue, import.Diagnostics.Single().Kind);
        Assert.Contains(new UnmodelledProperty("List", "$\"Form1.frx\":0000"), import.Form!.Find("List1")!.UnmodelledProperties);
    }

    [TestMethod]
    public void AnEmptyList_IsTwoBytes()
    {
        var import = VB6FormReader.Read(Form("   Begin VB.ListBox List1 ", "      List            =   \"Form1.frx\":0000", "   End"),
            Bytes(0x00, 0x00));

        Assert.IsEmpty(import.Form!.Find("List1")!.Get(VBProperties.List));
        Assert.IsEmpty(import.Diagnostics);
    }

    [TestMethod]
    public void AnItemDataItemThatIsNoNumber_IsNoRecordTheItemDataCanRead()
    {
        var import = VB6FormReader.Read(Form("   Begin VB.ListBox List1 ", "      ItemData        =   \"Form1.frx\":0000", "   End"),
            Bytes(0x01, 0x00, 0x00, 0x00, 0x01, 0x00, (byte)'x'));

        Assert.AreEqual(VB6ImportDiagnosticKind.MissingRecord, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AFontBagThatLeavesFieldsOut_KeepsTheDefaultFontsForThem()
    {
        var import = VB6FormReader.Read(Form(Label("BeginProperty Font ", "   name = \"Arial\"", "   size = 12", "EndProperty")));

        Assert.AreEqual(FormFont.Default with { Name = "Arial", Size = 12 }, import.Form!.Find("Label1")!.Get(VBProperties.Font));
    }

    [TestMethod]
    public void AFontBagWithAFieldItCannotRead_IsKeptAsText_AndTheFormsFontIsNotTakenAsWell()
    {
        var import = VB6FormReader.Read(Form(["   BeginProperty Font ", "      name = \"Arial\"", "   EndProperty",
            .. Label("BeginProperty Font ", "   Weight = Bold", "EndProperty")]));
        var label = import.Form!.Find("Label1")!;

        Assert.AreEqual("Arial", import.Form.Root.Get(VBProperties.Font).Name);
        Assert.AreEqual(VB6ImportDiagnosticKind.InvalidValue, import.Diagnostics.Single().Kind);
        Assert.AreEqual("Font", label.UnmodelledProperties.Single().Name);
        Assert.IsFalse(label.IsSet(VBProperties.Font));
    }

    [TestMethod]
    public void AQuoteWrittenTwice_IsOneQuote_AndAValuesCommentIsNotItsValue()
    {
        var import = VB6FormReader.Read(Form(Label("Caption         =   \"say \"\"hi\"\"\"", "Alignment       =   1  'Right Justify")));
        var label = import.Form!.Find("Label1")!;

        Assert.AreEqual("say \"hi\"", label.Get(VBProperties.Caption));
        Assert.AreEqual(TextAlignment.RightJustify, label.Get(VBProperties.Alignment));
    }

    [TestMethod]
    public void ABlockWithNoEnd_IsReported_AndWhatWasReadIsKept()
    {
        var import = VB6FormReader.Read("VERSION 5.00\r\nBegin VB.Form Form1 \r\n   Caption = \"x\"\r\n   Begin VB.Label Label1 \r\n");

        Assert.AreEqual("x", import.Form!.Root.Get(VBProperties.Caption));
        Assert.IsNotNull(import.Form.Find("Label1"));
        CollectionAssert.AreEqual(new[] { (VB6ImportDiagnosticKind.UnbalancedBlock, 2), (VB6ImportDiagnosticKind.UnbalancedBlock, 4) },
            import.Diagnostics.Select(d => (d.Kind, d.Line)).ToArray());
        Assert.AreEqual("", import.ModuleText);
    }

    [TestMethod]
    public void AnEndPropertyWithNoBeginProperty_IsReportedAsClosingNothing()
    {
        var import = VB6FormReader.Read(Form("   EndProperty"));

        Assert.AreEqual((VB6ImportDiagnosticKind.UnmatchedEnd, 6), (import.Diagnostics.Single().Kind, import.Diagnostics.Single().Line));
    }

    [TestMethod]
    public void ALineThatIsNoPropertyBeginOrEnd_IsReported()
    {
        var import = VB6FormReader.Read(Form("   Caption"));

        Assert.AreEqual(VB6ImportDiagnosticKind.MalformedLine, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void LinesEndingInALoneLineFeed_AreLinesToo_AndTheModuleTextKeepsItsOwnEndings()
    {
        var import = VB6FormReader.Read("VERSION 5.00\nBegin VB.Form Form1 \n   Caption = \"x\"\nEnd\nAttribute VB_Name = \"Form1\"\r\nSub A()\n");

        Assert.AreEqual("x", import.Form!.Root.Get(VBProperties.Caption));
        Assert.AreEqual("Attribute VB_Name = \"Form1\"\r\nSub A()\n", import.ModuleText);
        Assert.AreEqual(5, import.ModuleLine);
    }

    [TestMethod]
    public void ALibraryNamedByAProjectPath_IsReported_AndNotListed()
    {
        var import = VB6FormReader.Read("VERSION 5.00\r\nObject = *\\A..\\Controls\\Controls.vbp\r\n" + Form()[14..]);

        Assert.IsEmpty(import.Form!.ControlLibraries);
        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual((VB6ImportDiagnosticKind.UnreadLibraryReference, VB6ImportSeverity.Information, 2),
            (diagnostic.Kind, diagnostic.Severity, diagnostic.Line));
    }

    [TestMethod]
    public void AMenuInsideAControl_IsPlacedInTheForm()
    {
        var import = VB6FormReader.Read(Form("   Begin VB.Frame Frame1 ", "      Begin VB.Menu mnuFile ", "      End", "   End"));

        Assert.AreSame(import.Form!.Root, import.Form.Find("mnuFile")!.Container);
        Assert.AreEqual(VB6ImportDiagnosticKind.MisplacedComponent, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AnIndexThatIsNoNumber_IsReported_KeptAsText_AndTheComponentIsNoArrayElement()
    {
        var import = VB6FormReader.Read(Form(Label("Index           =   \"a\"")));
        var label = import.Form!.Find("Label1")!;

        Assert.IsNull(label.Index);
        Assert.Contains(new UnmodelledProperty("Index", "\"a\""), label.UnmodelledProperties);
        Assert.AreEqual(VB6ImportDiagnosticKind.InvalidValue, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AMessage_IsInTheCurrentUICulture()
    {
        var culture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("fr-CA");
            var french = VB6FormReader.Read(Form(Label("Alignment = 7"))).Diagnostics.Single().Message;
            CultureInfo.CurrentUICulture = new CultureInfo("en-CA");
            var english = VB6FormReader.Read(Form(Label("Alignment = 7"))).Diagnostics.Single().Message;

            Assert.AreEqual("La propriété Alignment de Label1 a une valeur non valide; elle est donc conservée sous forme de texte.", french);
            Assert.AreEqual("Property Alignment in Label1 has an invalid value, so it is kept as text.", english);
        }
        finally
        {
            CultureInfo.CurrentUICulture = culture;
        }
    }
}

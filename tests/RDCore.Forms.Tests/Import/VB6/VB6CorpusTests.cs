using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Import.VB6;

// Forms VB6's IDE wrote, from HexIDE's corpus: in-box ActiveX controls, which the model does not have, beside intrinsic ones.
[TestClass]
public sealed class VB6CorpusTests
{
    private static VB6FormImport Read(params string[] path) =>
        VB6FormReader.ReadFile(Path.Combine([AppContext.BaseDirectory, "Import", "VB6", "Fixtures", "HexIDE", .. path]));

    [TestMethod]
    public void AFormOfActiveXControls_KeepsEachAsText_AndListsItsLibrary()
    {
        var import = Read("five", "Form1.frm");
        var form = import.Form!;

        Assert.AreEqual("Saved by VB6", form.Root.Get(VBProperties.Caption));
        Assert.AreEqual(StartUpPosition.WindowsDefault, form.Root.Get(VBProperties.StartUpPosition));
        Assert.IsEmpty(form.Root.Contents);
        Assert.HasCount(8, form.Root.UnmodelledContents);
        Assert.AreEqual(new ControlLibraryReference(new Guid("6B7E6392-850A-101B-AFC0-4210102A8DA7"), "1.3", "COMCTL32.OCX"),
            form.ControlLibraries.Single());
        Assert.IsTrue(import.Diagnostics.All(d => d is { Kind: VB6ImportDiagnosticKind.UnmodelledClass, Severity: VB6ImportSeverity.Information }));
        Assert.HasCount(8, import.Diagnostics);
    }

    [TestMethod]
    public void AnActiveXControlKeptAsText_IsItsBlockAsWritten()
    {
        var bar = Read("five", "Form1.frm").Form!.Root.UnmodelledContents[0];

        Assert.AreEqual(("ComctlLib.ProgressBar", "pbDef", 0), (bar.TypeName, bar.Name, bar.Position));
        Assert.StartsWith("   Begin ComctlLib.ProgressBar pbDef ", bar.Text);
        Assert.EndsWith("\r\n   End", bar.Text);
        Assert.Contains("      _Version        =   327682", bar.Text);
    }

    [TestMethod]
    public void AnActiveXControlsPropertyBags_StayInItsText()
    {
        var images = Read("imagelist-three", "Form1.frm").Form!.Root.UnmodelledContents.Single();

        Assert.AreEqual("MSComctlLib.ImageList", images.TypeName);
        Assert.Contains("            Picture         =   \"Form1.frx\":0352", images.Text);
        Assert.Contains("      EndProperty", images.Text);
    }

    [TestMethod]
    public void AnActiveXControlsCompanionRecords_AreNotReadAsAnIntrinsicsWouldBe()
    {
        var import = Read("richtextbox", "Form1.frm");

        Assert.HasCount(3, import.Form!.Root.UnmodelledContents);
        Assert.IsTrue(import.Diagnostics.All(d => d.Kind == VB6ImportDiagnosticKind.UnmodelledClass));
    }

    [TestMethod]
    public void TheIntrinsicControlsAmongActiveXOnes_AreModelled_AndEachKeptControlRemembersWhereItWas()
    {
        var form = Read("updown", "Form1.frm").Form!;

        CollectionAssert.AreEqual(new[] { "Text1", "Text2" }, form.Root.Contents.Select(c => c.Name).ToArray());
        Assert.AreEqual("Text2", form.Find("Text2")!.Get(VBProperties.Text));
        CollectionAssert.AreEqual(new[] { ("UpDown4", 0), ("UpDown1", 1), ("UpDown2", 1), ("UpDown3", 2) },
            form.Root.UnmodelledContents.Select(c => (c.Name, c.Position)).ToArray());
    }

    [TestMethod]
    public void TheModuleText_IsEverythingAfterTheDesignerBlock_AttributesIncluded()
    {
        var import = Read("member-attributes", "Main.frm");

        Assert.AreEqual(13, import.ModuleLine);
        Assert.StartsWith("Attribute VB_Name = \"Main\"\r\n", import.ModuleText);
        Assert.Contains("Attribute mLedger.VB_VarHelpID = -1\r\n", import.ModuleText);
        Assert.IsEmpty(import.Diagnostics);
    }
}

using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Import.VB6;

// Forms VB6's IDE wrote, from HexIDE's corpus: in-box ActiveX controls beside intrinsic ones. The model has none of the ActiveX ones
// but MSComctlLib's ImageList.
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

        Assert.AreEqual("Saved by VB6", form.Root.Get(Properties.Caption));
        Assert.AreEqual(StartUpPosition.WindowsDefault, form.Root.Get(Properties.StartUpPosition));
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
        var rich = Read("richtextbox", "Form1.frm").Form!.Root.UnmodelledContents.Single(c => c.Name == "RichTextBox1");

        Assert.AreEqual("RichTextLib.RichTextBox", rich.TypeName);
        Assert.Contains("      BeginProperty Font {0BE35203-8F91-11CE-9DE3-00AA004BB851} ", rich.Text);
        Assert.Contains("      EndProperty", rich.Text);
    }

    // The ImageList, MSComctlLib's, is modelled: each ListImage's record is the length of the rest, StdPicture's CLSID, "lt\0\0", the
    // image's length and the image, here two 16x16 bitmaps and an icon.
    [TestMethod]
    public void AnImageListVB6Saved_IsReadWithItsPictures_TheirKeysAndTags()
    {
        var import = Read("imagelist-three", "Form1.frm");
        var imageList = import.Form!.Find("ImageList1")!;
        var images = imageList.Get(Properties.ListImages);

        Assert.IsEmpty(import.Diagnostics);
        Assert.IsEmpty(import.Form.Root.UnmodelledContents);
        Assert.AreEqual(((short)16, (short)16), (imageList.Get(Properties.ImageWidth), imageList.Get(Properties.ImageHeight)));
        CollectionAssert.AreEqual(new[] { ("red", "t1"), ("blue", ""), ("ico", "t3") }, images.Select(i => (i.Key, i.Tag)).ToArray());
        CollectionAssert.AreEqual(new[] { 0x336, 0x336, 0x1BE }, images.Select(i => i.Picture.Length).ToArray());
        CollectionAssert.AreEqual(new[] { "424D", "424D", "0000" }, images.Select(i => Convert.ToHexString(i.Picture, 0, 2)).ToArray());
        Assert.IsFalse(imageList.UnmodelledProperties.Any(p => p.Name == "Images"));
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
        Assert.AreEqual("Text2", form.Find("Text2")!.Get(Properties.Text));
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

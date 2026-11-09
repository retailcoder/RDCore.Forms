using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Import.VB6;

// ReadFile finds each companion file by the name the designer file cites, in the designer file's folder.
[TestClass]
public sealed class VB6CompanionFileTests
{
    private string folder = "";

    [TestInitialize]
    public void CreateFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "rdcore-forms-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(folder, recursive: true);

    private string WriteForm(string citedFile)
    {
        var path = Path.Combine(folder, "Form1.frm");
        File.WriteAllText(path, string.Join("\r\n",
            "VERSION 5.00",
            "Begin VB.Form Form1 ",
            "   Begin VB.Label Label1 ",
            $"      Caption         =   \"{citedFile}\":0000",
            "   End",
            "End",
            ""));
        return path;
    }

    // A short string record: a 1-byte length, then "ok".
    private void WriteCompanion(string name) => File.WriteAllBytes(Path.Combine(folder, name), [0x02, (byte)'o', (byte)'k']);

    [TestMethod]
    public void ACompanionIsTheFileTheDesignerFileCites_NotTheOneNamedAfterIt()
    {
        var form = WriteForm("Other.frx");
        WriteCompanion("Other.frx");

        var import = VB6FormReader.ReadFile(form);

        Assert.AreEqual("ok", import.Form!.Find("Label1")!.Get(VBProperties.Caption));
        Assert.IsEmpty(import.Diagnostics);
    }

    [TestMethod]
    public void ACompanionsName_IsComparedWithoutRegardToCase()
    {
        var form = WriteForm("Form1.frx");
        WriteCompanion("FORM1.FRX");

        var import = VB6FormReader.ReadFile(form);

        Assert.AreEqual("ok", import.Form!.Find("Label1")!.Get(VBProperties.Caption));
    }

    [TestMethod]
    public void ARootedCitation_IsNotRead_EvenWhenTheFileExists()
    {
        var rooted = Path.Combine(folder, "Rooted.frx");
        var form = WriteForm(rooted);
        WriteCompanion("Rooted.frx");

        var import = VB6FormReader.ReadFile(form);

        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual(VB6ImportDiagnosticKind.MissingCompanion, diagnostic.Kind);
        Assert.Contains(rooted, diagnostic.Message);
        Assert.AreEqual("", import.Form!.Find("Label1")!.Get(VBProperties.Caption));
    }

    [TestMethod]
    public void ACitationWithANul_IsAMissingCompanion_AndDoesNotThrow()
    {
        var form = WriteForm("Form1\0.frx");

        var import = VB6FormReader.ReadFile(form);

        Assert.AreEqual(VB6ImportDiagnosticKind.MissingCompanion, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AMissingCompanion_IsReportedWithThePathLookedFor()
    {
        var form = WriteForm("Missing.frx");

        var import = VB6FormReader.ReadFile(form);

        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual(VB6ImportDiagnosticKind.MissingCompanion, diagnostic.Kind);
        Assert.Contains(Path.Combine(folder, "Missing.frx"), diagnostic.Message);
    }
}

using System.Text;
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Import.VB6;

// What a read makes of a file VB6 would not write: it reports it, keeps what it cannot model as text, and throws nothing.
[TestClass]
public sealed class VB6MalformedFileTests
{
    private static string Form(params string[] contents) =>
        string.Join("\r\n", ["VERSION 5.00", "Begin VB.Form Form1 ", .. contents, "End", "Attribute VB_Name = \"Form1\"", ""]);

    private static string[] Label(params string[] properties) =>
        ["   Begin VB.Label Label1 ", .. properties.Select(p => "      " + p), "   End"];

    // The form, then frames nested inside it until the blocks are depth deep, the form's included.
    private static string NestedFrames(int depth, bool closed = true)
    {
        var text = new StringBuilder("VERSION 5.00\r\nBegin VB.Form Form1 \r\n");
        for (var level = 2; level <= depth; level++)
        {
            text.Append("Begin VB.Frame Frame").Append(level).Append(" \r\n");
        }

        if (closed)
        {
            for (var level = 1; level <= depth; level++)
            {
                text.Append("End\r\n");
            }
        }

        return text.ToString();
    }

    private static T OnANewThread<T>(Func<T> read)
    {
        var result = default(T)!;
        var thread = new Thread(() => result = read());
        thread.Start();
        thread.Join();
        return result;
    }

    [TestMethod]
    public void AChildBlockWithNoName_IsKeptAsText_AndReported()
    {
        var import = VB6FormReader.Read(Form("   Begin VB.Label ", "      Caption = \"x\"", "   End"));

        var kept = import.Form!.Root.UnmodelledContents.Single();
        Assert.AreEqual(("VB.Label", ""), (kept.TypeName, kept.Name));
        Assert.StartsWith("   Begin VB.Label", kept.Text);
        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual((VB6ImportDiagnosticKind.NamelessComponent, VB6ImportSeverity.Warning, 3),
            (diagnostic.Kind, diagnostic.Severity, diagnostic.Line));
    }

    [TestMethod]
    public void ARootBlockWithNoName_IsKeptAsText_AndLeavesNoForm()
    {
        var import = VB6FormReader.Read("VERSION 5.00\r\nBegin VB.Form \r\n   Caption = \"x\"\r\nEnd\r\nAttribute VB_Name = \"Form1\"\r\n");

        Assert.IsNull(import.Form);
        Assert.AreEqual(new UnmodelledComponent("VB.Form", "", 0, "Begin VB.Form \r\n   Caption = \"x\"\r\nEnd"), import.UnmodelledRoot);
        Assert.AreEqual("Attribute VB_Name = \"Form1\"\r\n", import.ModuleText);
        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual((VB6ImportDiagnosticKind.NamelessComponent, VB6ImportSeverity.Error, 2),
            (diagnostic.Kind, diagnostic.Severity, diagnostic.Line));
    }

    [TestMethod]
    public void BlocksNestedAsDeepAsTheLimit_AreAllRead_OnTheDefaultStack()
    {
        var text = NestedFrames(VB6FormReader.MaxNestingDepth);

        var (descendants, deepest, diagnostics) = OnANewThread(() =>
        {
            var import = VB6FormReader.Read(text);
            return (import.Form!.Components.Count() - 1, import.Form.Find($"Frame{VB6FormReader.MaxNestingDepth}"), import.Diagnostics);
        });

        Assert.AreEqual(VB6FormReader.MaxNestingDepth - 1, descendants);
        Assert.IsNotNull(deepest);
        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public void ABlockNestedPastTheLimit_IsKeptAsText_WithItsContents()
    {
        var depth = VB6FormReader.MaxNestingDepth;
        var import = VB6FormReader.Read(NestedFrames(depth + 2));

        var container = import.Form!.Find($"Frame{depth}")!;
        var kept = container.UnmodelledContents.Single();
        Assert.AreEqual(("VB.Frame", $"Frame{depth + 1}"), (kept.TypeName, kept.Name));
        Assert.Contains($"Begin VB.Frame Frame{depth + 2}", kept.Text);
        Assert.IsNull(import.Form.Find($"Frame{depth + 1}"));
        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual((VB6ImportDiagnosticKind.NestingTooDeep, depth + 2), (diagnostic.Kind, diagnostic.Line));
    }

    [TestMethod]
    public void AHundredThousandBeginsWithNoEnd_AreRead_AndEachIsReported()
    {
        const int depth = 100_000;

        var import = VB6FormReader.Read(NestedFrames(depth, closed: false));

        Assert.HasCount(VB6FormReader.MaxNestingDepth - 1, import.Form!.Root.Descendants().ToArray());
        Assert.AreEqual(depth, import.Diagnostics.Count(d => d.Kind == VB6ImportDiagnosticKind.UnbalancedBlock));
        Assert.AreEqual(1, import.Diagnostics.Count(d => d.Kind == VB6ImportDiagnosticKind.NestingTooDeep));
    }

    [TestMethod]
    [DataRow("Left = NaN", DisplayName = "not a number")]
    [DataRow("Enabled = NaN", DisplayName = "not a number for a Boolean")]
    [DataRow("Left = Infinity", DisplayName = "an infinity")]
    [DataRow("Left = 1E400", DisplayName = "past a Double's range")]
    [DataRow("Left = 1E39", DisplayName = "past a Single's range")]
    [DataRow("TabIndex = 40000", DisplayName = "past an Integer's range")]
    [DataRow("WhatsThisHelpID = 3000000000", DisplayName = "past a Long's range")]
    public void ANumberPastItsVBTypesRange_IsKeptAsText_AndReported(string property)
    {
        var import = VB6FormReader.Read(Form(Label(property)));
        var label = import.Form!.Find("Label1")!;

        Assert.HasCount(1, label.UnmodelledProperties);
        Assert.AreEqual(VB6ImportDiagnosticKind.InvalidValue, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void AnIntegerProperty_IsReadAsAShort()
    {
        var import = VB6FormReader.Read(Form(Label("TabIndex = 32767")));

        Assert.AreEqual(short.MaxValue, import.Form!.Find("Label1")!.Get(VBProperties.TabIndex));
    }

    [TestMethod]
    public void AnMDIForm_IsARoot_AndItsControlsAndMenusAreRead()
    {
        var import = VB6FormReader.Read(string.Join("\r\n",
            "VERSION 5.00",
            "Begin VB.MDIForm MDIForm1 ",
            "   BackColor       =   &H8000000C&",
            "   Caption         =   \"Main\"",
            "   ClientWidth     =   6000",
            "   Begin VB.PictureBox picBar ",
            "      Align           =   1  'Align Top",
            "   End",
            "   Begin VB.Menu mnuFile ",
            "      Caption         =   \"&File\"",
            "   End",
            "End",
            ""));
        var form = import.Form!;

        Assert.AreSame(VBClasses.MDIForm, form.Root.Class);
        Assert.AreEqual(("Main", 6000d), (form.Root.Get(VBProperties.Caption), form.Root.Get(VBProperties.Width)));
        CollectionAssert.AreEqual(new[] { "picBar", "mnuFile" }, form.Root.Contents.Select(c => c.Name).ToArray());
        Assert.IsEmpty(import.Diagnostics);
    }

    [TestMethod]
    public void TheDiagnostics_AreInTheOrderOfTheirLines()
    {
        // the unclosed blocks are found at the end of the file, after the malformed line they come before.
        var import = VB6FormReader.Read("VERSION 5.00\r\nBegin VB.Form Form1 \r\n   Begin VB.Label Label1 \r\n   Caption\r\n");

        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, import.Diagnostics.Select(d => d.Line).ToArray());
    }

    [TestMethod]
    public void APropertyBagWithNoEndProperty_KeepsItsLines_UpToTheEndThatClosesItsComponent()
    {
        var import = VB6FormReader.Read(Form(Label("BeginProperty DataFormat ", "   Type = 1")));

        var bag = import.Form!.Find("Label1")!.UnmodelledProperties.Single();
        Assert.AreEqual(("DataFormat", "BeginProperty DataFormat \r\n         Type = 1"), (bag.Name, bag.Text.TrimStart()));
        Assert.AreEqual(VB6ImportDiagnosticKind.UnbalancedBlock, import.Diagnostics.Single().Kind);
    }

    [TestMethod]
    public void ANumberPropertyCitingACompanionRecord_IsAValueItCannotHold()
    {
        var import = VB6FormReader.Read(Form(Label("Left = \"Form1.frx\":0000")), [0x01, 0x00]);

        Assert.AreEqual(VB6ImportDiagnosticKind.InvalidValue, import.Diagnostics.Single().Kind);
        Assert.Contains(new UnmodelledProperty("Left", "\"Form1.frx\":0000"), import.Form!.Find("Label1")!.UnmodelledProperties);
    }

    [TestMethod]
    public void AHeaderLineThatNamesNoLibrary_IsKeptAsText()
    {
        var import = VB6FormReader.Read("VERSION 5.00\r\nReference = \"x\"\r\nBegin VB.Form Form1 \r\nEnd\r\n");

        Assert.AreEqual(new UnmodelledProperty("Reference", "\"x\""), import.UnmodelledHeader.Single());
        var diagnostic = import.Diagnostics.Single();
        Assert.AreEqual((VB6ImportDiagnosticKind.UnmodelledHeaderLine, VB6ImportSeverity.Information, 2),
            (diagnostic.Kind, diagnostic.Severity, diagnostic.Line));
    }
}

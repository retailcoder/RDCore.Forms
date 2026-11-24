using RDCore.Forms.Xaml;

namespace RDCore.Forms.Tests.Xaml;

[TestClass]
public sealed class FormXamlNameRangesTests
{
    private const string Ns = FormXaml.Namespace;

    private static string Document(string body, string newLine = "\n") =>
        string.Join(newLine, $"<Form xmlns=\"{Ns}\" FormatVersion=\"1\" Name=\"Form1\">", body, "</Form>");

    private static string[] Texts(string document, IEnumerable<FormXamlNameRange> ranges) =>
        [.. ranges.Select(range => document.Substring(range.Offset, range.Length))];

    // The line and column a range starts at, counted from the text itself.
    private static (int Line, int Column) PositionOf(string document, int offset)
    {
        var lines = document[..offset].Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        return (lines.Length, lines[^1].Length + 1);
    }

    [TestMethod]
    public void EachComponentOfTheName_IsFound_EveryElementOfAControlArrayIncluded()
    {
        var document = Document("""
              <CommandButton Name="Command1" Index="0" Caption="Command1" />
              <Frame Name="Frame1">
                <CommandButton Name="command1" Index="1" />
              </Frame>
              <TextBox Name="Text1" />
            """);

        var ranges = FormXaml.NameRangesOf(document, "COMMAND1");

        CollectionAssert.AreEqual(new[] { "Command1", "command1" }, Texts(document, ranges));
        CollectionAssert.AreEqual(new[] { (2, 24), (4, 26) }, ranges.Select(range => (range.Line, range.Column)).ToArray());
    }

    [TestMethod]
    public void TheRootAndAMenuItem_AreFound()
    {
        var document = Document("""<Menu Name="mnuFile"><Menu Name="mnuOpen" /></Menu>""");

        Assert.AreEqual("Form1", Texts(document, FormXaml.NameRangesOf(document, "Form1")).Single());
        Assert.AreEqual("mnuOpen", Texts(document, FormXaml.NameRangesOf(document, "mnuOpen")).Single());
    }

    [TestMethod]
    public void ANameKeptAsText_IsNotFound()
    {
        var document = Document("""
              <!-- <TextBox Name="Text1" /> -->
              <UnmodelledComponent TypeName="MSComctlLib.Slider" Name="Text1">Begin MSComctlLib.Slider Text1</UnmodelledComponent>
              <Label Name="Label1">
                <Label.UnmodelledProperties><UnmodelledProperty Name="Text1" Text="Text1" /></Label.UnmodelledProperties>
              </Label>
              <TabStrip Name="TabStrip1"><TabStrip.Tabs><Tab Name="Text1" Caption="Text1" /></TabStrip.Tabs></TabStrip>
              <TextBox Name="Text1" />
            """);

        var ranges = FormXaml.NameRangesOf(document, "Text1");

        Assert.AreEqual((8, 18), (ranges.Single().Line, ranges.Single().Column));
    }

    [TestMethod]
    [DataRow("\n", DisplayName = "LF")]
    [DataRow("\r\n", DisplayName = "CR LF")]
    [DataRow("\r", DisplayName = "CR")]
    public void ARange_IsTheValueAsTheTextWritesIt_WhereverItsQuotesAre(string newLine)
    {
        var document = Document(string.Join(newLine, "  <TextBox", "    Name =", "\t'Te&#x78;t1' />", "  <TextBox Name=\"Text2\" />"), newLine);

        var range = FormXaml.NameRangesOf(document, "Text1").Single();

        Assert.AreEqual("Te&#x78;t1", document.Substring(range.Offset, range.Length));
        Assert.AreEqual(PositionOf(document, range.Offset), (range.Line, range.Column));
        Assert.AreEqual((4, 3), (range.Line, range.Column));
    }

    [TestMethod]
    public void TheRangesReplacedWithANewName_ReadAsTheComponentsRenamed()
    {
        var document = Document("""
              <OptionButton Name="Option1" Index="0" />
              <OptionButton Name="Option1" Index="1" />
              <OptionButton Name="Option2" />
            """);

        var renamed = document;
        foreach (var range in FormXaml.NameRangesOf(document, "Option1").Reverse())
            renamed = string.Concat(renamed.AsSpan(0, range.Offset), "Choice", renamed.AsSpan(range.Offset + range.Length));

        var read = FormXaml.Read(renamed);
        Assert.IsEmpty(read.Diagnostics);
        CollectionAssert.AreEqual(new[] { "Choice(0)", "Choice(1)", "Option2" },
            read.Form!.Root.Contents.Select(c => c.Index is { } index ? $"{c.Name}({index})" : c.Name).ToArray());
    }

    [TestMethod]
    [DataRow("<Form", DisplayName = "not well-formed")]
    [DataRow("<Window xmlns=\"https://rubberduckvba.ca/schemas/rdforms\" Name=\"Text1\" />", DisplayName = "no form")]
    [DataRow("<Form xmlns=\"https://rubberduckvba.ca/schemas/rdforms\" FormatVersion=\"1\" Name=\"Form1\" />", DisplayName = "no such name")]
    public void ADocumentWithNoComponentOfTheName_HasNoRange(string document) =>
        Assert.IsEmpty(FormXaml.NameRangesOf(document, "Text1"));
}

using RDCore.Forms.Import.VB6;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Xaml;
using static RDCore.Forms.Tests.Model.TestPictures;
using K = RDCore.Forms.Xaml.FormXamlDiagnosticKind;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Xaml;

[TestClass]
public sealed class FormXamlTests
{
    private const string Ns = FormXaml.Namespace;

    private static string Fixture(params string[] path) => Path.Combine([AppContext.BaseDirectory, "Import", "VB6", "Fixtures", .. path]);

    private static FormXamlRead Read(string body, string root = "Form", string attributes = "") =>
        FormXaml.Read($"<{root} xmlns=\"{Ns}\" FormatVersion=\"1\" Name=\"Form1\"{attributes}>{body}</{root}>");

    private static string[] Kinds(FormXamlRead read) => [.. read.Diagnostics.Select(d => $"{d.Kind} {d.Severity}")];

    [TestMethod]
    [DataRow("Intrinsics.frm")]
    [DataRow("Misplaced.frm")]
    [DataRow("UserScale.frm")]
    [DataRow("HexIDE", "five", "Form1.frm")]
    [DataRow("HexIDE", "imagelist-three", "Form1.frm")]
    [DataRow("HexIDE", "member-attributes", "Main.frm")]
    [DataRow("HexIDE", "richtextbox", "Form1.frm")]
    [DataRow("HexIDE", "updown", "Form1.frm")]
    public void AnImportedForm_IsWrittenAndReadBack_WithNothingReportedAndNothingLost(params string[] path)
    {
        var form = VB6FormReader.ReadFile(Fixture(path)).Form!;
        var written = FormXaml.Write(form);

        var read = FormXaml.Read(written);

        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        Assert.AreEqual(FormXaml.FormatVersion, read.FormatVersion);
        Assert.AreEqual(written, FormXaml.Write(read.Form!));
        Assert.AreEqual(form.Components.Count(), read.Form!.Components.Count());
    }

    [TestMethod]
    public void AnImportedFormsValues_ComeBackAsTheyWere()
    {
        var form = VB6FormReader.ReadFile(Fixture("Intrinsics.frm")).Form!;

        var read = FormXaml.Read(FormXaml.Write(form)).Form!;

        foreach (var (before, after) in form.Components.Zip(read.Components))
        {
            Assert.AreEqual((before.Class, before.Name, before.Index), (after.Class, after.Name, after.Index));
            Assert.AreEqual(before.Container?.Name, after.Container?.Name);
            // A picture set to none is the default, and is not written.
            CollectionAssert.AreEquivalent(before.SetValues.Where(v => v.Value is not null).Select(v => v.Key).ToList(), after.SetValues.Keys.ToList(), before.Name);
            foreach (var (property, value) in before.SetValues)
                Assert.AreEqual(FormXamlTestValues.Text(value), FormXamlTestValues.Text(after.GetBoxed(property)), $"{before.Name}.{property.Name}");
            CollectionAssert.AreEqual(before.UnmodelledProperties, after.UnmodelledProperties, before.Name);
        }

        CollectionAssert.AreEqual(form.ControlLibraries, read.ControlLibraries);
    }

    [TestMethod]
    public void AMenusShortcut_IsWrittenAsVBWritesIt_AndReadBack()
    {
        var form = VB6FormReader.ReadFile(Fixture("Intrinsics.frm")).Form!;

        var written = FormXaml.Write(form);
        var read = FormXaml.Read(written);

        StringAssert.Contains(written, "Shortcut=\"^O\"");
        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        Assert.AreEqual(new MenuShortcut(79, ShortcutModifiers.Ctrl),
            read.Form!.Components.Single(c => c.Name == "mnuFileOpen").Get(P.Shortcut));
    }

    [TestMethod]
    [DataRow("+{F2}", (short)113, ShortcutModifiers.Shift)]
    [DataRow("+^{DEL}", (short)46, ShortcutModifiers.Shift | ShortcutModifiers.Ctrl)]
    public void AShortcutAttribute_IsReadAsVBWritesIt(string text, short keyCode, ShortcutModifiers modifiers)
    {
        var read = Read($"<Menu Name=\"mnuA\" Caption=\"A\" Shortcut=\"{text}\" />");

        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        Assert.AreEqual(new MenuShortcut(keyCode, modifiers), read.Form!.Components.Single(c => c.Name == "mnuA").Get(P.Shortcut));
    }

    [TestMethod]
    public void AForm_IsWrittenAsTheFormatHasIt()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Caption, "Hello").Set(P.Width, 320.0)
            .Set(P.BackColor, FormColor.FromSystem(SystemColor.ButtonFace)).Set(P.ForeColor, FormColor.FromRgb(0x12, 0x34, 0xAB));
        var form = new FormDefinition(root);
        var button = new ComponentInstance(VBClasses.CommandButton, "Command1").Set(P.Caption, "&OK").Set(P.Font, new FormFont("Tahoma", 9, FormFont.BoldWeight));
        button.SetContainer(root);
        var option = new ComponentInstance(VBClasses.OptionButton, "Option1", 2).Set(P.OptionButtonValue, true);
        option.SetContainer(root);
        var list = new ComponentInstance(VBClasses.ListBox, "List1").Set(P.List, ["a", "b"]).Set(P.ItemData, [7]).Set(P.ItemIcon, [null, Gif1x1])
            .Set(P.MultiSelect, ListBoxMultiSelect.Extended);
        list.SetContainer(root);
        root.UnmodelledContents.Add(new UnmodelledComponent("MSComctlLib.ListViewCtrl", "ListView1", 1, "Begin MSComctlLib.ListView ListView1\r\nEnd"));

        var written = FormXaml.Write(form);

        Assert.AreEqual(
            $"""
            <Form FormatVersion="1" Name="Form1" Caption="Hello" BackColor="ButtonFace" ForeColor="#1234AB" Width="320" xmlns="{Ns}">
              <CommandButton Name="Command1" Caption="&amp;OK">
                <CommandButton.Font>
                  <Font Name="Tahoma" Size="9" Weight="700" />
                </CommandButton.Font>
              </CommandButton>
              <UnmodelledComponent TypeName="MSComctlLib.ListViewCtrl" Name="ListView1">Begin MSComctlLib.ListView ListView1&#xD;
            End</UnmodelledComponent>
              <OptionButton Name="Option1" Index="2" Value="True" />
              <ListBox Name="List1" MultiSelect="Extended">
                <ListBox.List>
                  <Item Text="a" ItemData="7" />
                  <Item Text="b" ItemIcon="{Convert.ToBase64String(Gif1x1)}" />
                </ListBox.List>
              </ListBox>
            </Form>
            """.ReplaceLineEndings("\n"), written);
    }

    [TestMethod]
    [DataRow("two\r\nlines")]
    [DataRow("a\ttab")]
    [DataRow("  spaced  ")]
    [DataRow("nul\0and\u0001control")]
    [DataRow("<&\"'>")]
    [DataRow("")]
    public void AStringOfAnyCharacters_ComesBackAsItWas(string text)
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Caption, text);
        var list = new ComponentInstance(VBClasses.ListBox, "List1").Set(P.List, [text]);
        list.SetContainer(root);
        root.UnmodelledProperties.Add(new UnmodelledProperty("Odd", text));
        root.UnmodelledContents.Add(new UnmodelledComponent("X.Y", "Y1", 0, text));

        var read = FormXaml.Read(FormXaml.Write(new FormDefinition(root)));

        Assert.IsEmpty(read.Diagnostics);
        var form = read.Form!.Root;
        Assert.AreEqual(text, form.Get(P.Caption));
        Assert.AreEqual(text, form.Contents.Single().Get(P.List).Single());
        Assert.AreEqual(text, form.UnmodelledProperties.Single().Text);
        Assert.AreEqual(text, form.UnmodelledContents.Single().Text);
    }

    [TestMethod]
    public void AFile_IsWrittenAsUtf8WithADeclaration_AndReadBack()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rdforms-{Guid.NewGuid():N}.xaml");
        try
        {
            FormXaml.WriteFile(new FormDefinition(new ComponentInstance(VBClasses.Form, "Form1").Set(P.Caption, "é")), path);

            var bytes = File.ReadAllBytes(path);
            var read = FormXaml.ReadFile(path);

            Assert.AreEqual("<?xml", System.Text.Encoding.ASCII.GetString(bytes, 0, 5));
            Assert.AreEqual("é", read.Form!.Root.Get(P.Caption));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    [DataRow("", DisplayName = "an empty document")]
    [DataRow("<Form", DisplayName = "an element left open")]
    [DataRow("<!DOCTYPE Form [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;\">]><Form>&b;</Form>", DisplayName = "a DTD")]
    [DataRow("<Form xmlns=\"https://rubberduckvba.ca/schemas/rdforms\" Name=\"Form1\"><Label Name=\"a\" Name=\"b\" /></Form>", DisplayName = "an attribute twice")]
    public void ADocumentThatIsNotWellFormed_HoldsNoForm(string document)
    {
        var read = FormXaml.Read(document);

        Assert.IsNull(read.Form);
        CollectionAssert.AreEqual(new[] { "MalformedDocument Error" }, Kinds(read));
    }

    [TestMethod]
    [DataRow("<Form Name=\"Form1\" />", DisplayName = "no namespace")]
    [DataRow("<Window xmlns=\"https://rubberduckvba.ca/schemas/rdforms\" Name=\"Form1\" />", DisplayName = "an element of no class")]
    [DataRow("<CommandButton xmlns=\"https://rubberduckvba.ca/schemas/rdforms\" Name=\"Form1\" />", DisplayName = "a control")]
    [DataRow("<Window xmlns=\"https://github.com/avaloniaui\" />", DisplayName = "an Avalonia window")]
    public void ADocumentWhoseRootIsNotAFormsRoot_HoldsNoForm(string document)
    {
        var read = FormXaml.Read(document);

        Assert.IsNull(read.Form);
        CollectionAssert.AreEqual(new[] { "NotAForm Error" }, Kinds(read));
    }

    [TestMethod]
    public void ARootWithNoName_LeavesTheDocumentWithNoForm()
    {
        var read = FormXaml.Read($"<UserControl xmlns=\"{Ns}\" FormatVersion=\"1\" />");

        Assert.IsNull(read.Form);
        CollectionAssert.AreEqual(new[] { "NamelessComponent Error" }, Kinds(read));
    }

    [TestMethod]
    public void ALaterFormatVersion_IsReadAnyway_AndNoneThatCanBeRead_IsReported()
    {
        var later = FormXaml.Read($"<Form xmlns=\"{Ns}\" FormatVersion=\"7\" Name=\"Form1\" Caption=\"Hi\" />");
        var none = FormXaml.Read($"<Form xmlns=\"{Ns}\" Name=\"Form1\" />");

        Assert.AreEqual("Hi", later.Form!.Root.Get(P.Caption));
        Assert.AreEqual(7, later.FormatVersion);
        CollectionAssert.AreEqual(new[] { "NewerFormatVersion Information" }, Kinds(later));
        Assert.IsNotNull(none.Form);
        Assert.AreEqual(0, none.FormatVersion);
        CollectionAssert.AreEqual(new[] { "InvalidValue Warning" }, Kinds(none));
    }

    [TestMethod]
    public void WhatTheModelCannotHold_IsKeptAsText_AndReported()
    {
        var read = Read(
            """
            <CommandButton Name="Command1" Width="wide" Shape="2" Index="40000" />
            <Label Name="Label1" BackColor="#GG0000" Alignment="Sideways" />
            <Grid Name="Grid1"><Button /></Grid>
            <TextBox Caption="nameless" />
            <TextBox Name="Text1"><TextBox.Font><Font Size="9" /></TextBox.Font><Label.Font /></TextBox>
            """);
        var form = read.Form!;
        var button = form.Find("Command1")!;

        CollectionAssert.AreEqual(new[]
        {
            "InvalidValue Warning", "UnknownProperty Warning", "InvalidValue Warning", "InvalidValue Warning", "InvalidValue Warning",
            "UnmodelledElement Information", "NamelessComponent Warning", "InvalidValue Warning", "UnmodelledElement Information",
        }, Kinds(read));
        CollectionAssert.AreEqual(new[] { new UnmodelledProperty("Width", "wide"), new UnmodelledProperty("Shape", "2"), new UnmodelledProperty("Index", "40000") },
            button.UnmodelledProperties);
        Assert.IsNull(button.Index);
        Assert.IsFalse(button.IsSet(P.Width));
        CollectionAssert.AreEqual(new[] { "Grid", "VB.TextBox" }, form.Root.UnmodelledContents.Select(c => c.TypeName.Replace($"{{{Ns}}}", "")).ToArray());
        Assert.AreEqual(2, form.Root.UnmodelledContents[1].Position);
        Assert.AreEqual("Font", form.Find("Text1")!.UnmodelledProperties.Single().Name);
    }

    [TestMethod]
    public void AComponentInOneThatCannotHoldIt_IsPlacedInTheNearestThatCan()
    {
        var read = Read(
            """
            <Frame Name="Frame1">
              <CommandButton Name="Command1"><Label Name="Label1" /></CommandButton>
              <Menu Name="mnuFile"><Menu Name="mnuOpen" /></Menu>
            </Frame>
            """);
        var form = read.Form!;

        CollectionAssert.AreEqual(new[] { "MisplacedComponent Warning", "MisplacedComponent Warning" }, Kinds(read));
        Assert.AreEqual("Frame1", form.Find("Label1")!.Container!.Name);
        Assert.AreEqual("Form1", form.Find("mnuFile")!.Container!.Name);
        Assert.AreEqual("mnuFile", form.Find("mnuOpen")!.Container!.Name);
    }

    [TestMethod]
    public void AComponentNestedTooDeep_IsKeptAsText_WithItsContents()
    {
        var depth = FormXaml.MaxNestingDepth + 10;
        var body = string.Concat(Enumerable.Range(1, depth).Select(i => $"<Frame Name=\"Frame{i}\">")) + string.Concat(Enumerable.Repeat("</Frame>", depth));

        var read = Read(body);
        var form = read.Form!;

        CollectionAssert.AreEqual(new[] { "NestingTooDeep Warning" }, Kinds(read));
        Assert.AreEqual(FormXaml.MaxNestingDepth, form.Components.Count());
        Assert.AreEqual($"Frame{FormXaml.MaxNestingDepth}", form.Find($"Frame{FormXaml.MaxNestingDepth - 1}")!.UnmodelledContents.Single().Name);
    }

    [TestMethod]
    public void AFormNestedToTheLimit_IsWrittenAndReadBack()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1");
        var container = root;
        for (var i = 1; i < FormXaml.MaxNestingDepth; i++)
        {
            var frame = new ComponentInstance(VBClasses.Frame, $"Frame{i}");
            frame.SetContainer(container);
            container = frame;
        }

        var read = FormXaml.Read(FormXaml.Write(new FormDefinition(root)));

        Assert.IsEmpty(read.Diagnostics);
        Assert.AreEqual(FormXaml.MaxNestingDepth, read.Form!.Components.Count());
    }

    [TestMethod]
    public void APropertyElement_HoldsItsValueAsText_AndAPictureIsItsFileInBase64()
    {
        var read = Read(
            $"""
            <Label Name="Label1"><Label.Caption>two
            lines</Label.Caption></Label>
            <Image Name="Image1" Picture="{Convert.ToBase64String(Bitmap2x2)}" />
            <Image Name="Image2" Picture="not base64!" />
            """);
        var form = read.Form!;

        Assert.AreEqual("two\nlines", form.Find("Label1")!.Get(P.Caption));
        CollectionAssert.AreEqual(Bitmap2x2, form.Find("Image1")!.Get(P.Picture));
        CollectionAssert.AreEqual(new[] { "InvalidValue Warning" }, Kinds(read));
    }

    [TestMethod]
    public void AListsItems_HoldTheirDataAndIcons_TheOnesLeftOutAre0AndNone()
    {
        var read = Read(
            $"""
            <ComboBox Name="Combo1"><ComboBox.List>
              <Item Text="a" />
              <Item Text="b" ItemData="-5" ItemIcon="{Convert.ToBase64String(Gif1x1)}" />
              <Item ItemData="x" />
            </ComboBox.List></ComboBox>
            """);
        var combo = read.Form!.Find("Combo1")!;

        CollectionAssert.AreEqual(new[] { "a", "b", "" }, combo.Get(P.List).ToArray());
        CollectionAssert.AreEqual(new[] { 0, -5, 0 }, combo.Get(P.ItemData).ToArray());
        CollectionAssert.AreEqual(new byte[]?[] { null, Gif1x1, null }, combo.Get(P.ItemIcon).ToArray(), new ByteArrayComparer());
        CollectionAssert.AreEqual(new[] { new UnmodelledProperty("ItemData(2)", "x") }, combo.UnmodelledProperties);
        CollectionAssert.AreEqual(new[] { "InvalidValue Warning" }, Kinds(read));
    }

    [TestMethod]
    public void EveryDocumentCutShortOrChanged_IsReadWithoutThrowing_AndAFormComesWithNoError()
    {
        var form = VB6FormReader.ReadFile(Fixture("Misplaced.frm")).Form!;
        var document = System.Text.Encoding.UTF8.GetBytes(FormXaml.Write(form));
        var read = 0;

        foreach (var bytes in Malformed(document, seed: 9, count: 1500))
        {
            var result = FormXaml.Read(System.Text.Encoding.UTF8.GetString(bytes));
            Assert.AreEqual(result.Form is null, result.Diagnostics.Any(d => d.Severity == FormXamlSeverity.Error));
            if (result.Form is { } readForm)
                Assert.IsTrue(readForm.Components.All(c => c.Class.Properties.Count > 0));
            read++;
        }

        Assert.IsGreaterThan(2000, read);
    }

    [TestMethod]
    public void TheFormatPagesExample_IsReadWithNothingReported()
    {
        var read = FormXaml.Read(
            """
            <Form FormatVersion="1" Name="Form1" Caption="Orders" Width="320" Height="240" xmlns="https://rubberduckvba.ca/schemas/rdforms">
              <Form.Font>
                <Font Name="Tahoma" Size="8.25" />
              </Form.Font>
              <Frame Name="Frame1" Caption="Ship by" Left="8" Top="8" Width="160" Height="80">
                <OptionButton Name="optShip" Index="0" Caption="&amp;Air" Value="True" Left="8" Top="16" Width="120" Height="20" />
                <OptionButton Name="optShip" Index="1" Caption="&amp;Sea" Left="8" Top="40" Width="120" Height="20" />
              </Frame>
              <ListBox Name="lstItems" Left="176" Top="8" Width="134" Height="80">
                <ListBox.List>
                  <Item Text="Alpha" ItemData="1" />
                  <Item Text="Beta" ItemData="20" ItemIcon="R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==" />
                </ListBox.List>
              </ListBox>
              <CommandButton Name="cmdOK" Caption="OK" Left="232" Top="192" Width="81" Height="33" />
            </Form>
            """);
        var form = read.Form!;

        Assert.IsEmpty(read.Diagnostics);
        Assert.AreEqual("&Sea", form.Find("optShip", 1)!.Get(P.Caption));
        Assert.AreEqual(new FormFont("Tahoma", 8.25), form.Root.Get(P.Font));
        Assert.IsNull(FormPictures.Inspect(form.Find("lstItems")!.Get(P.ItemIcon)[1], out _));
    }

    private sealed class ByteArrayComparer : System.Collections.IComparer
    {
        public int Compare(object? x, object? y) =>
            x is byte[] a && y is byte[] b ? (a.AsSpan().SequenceEqual(b) ? 0 : 1) : ReferenceEquals(x, y) || x is null && y is null ? 0 : 1;
    }
}

// A value as comparable text: a list or a picture by its contents.
internal static class FormXamlTestValues
{
    public static string? Text(object? value) => value switch
    {
        byte[] bytes => Convert.ToHexString(bytes),
        System.Collections.IEnumerable items and not string => string.Join("|", items.Cast<object?>().Select(Text)),
        _ => value?.ToString(),
    };
}

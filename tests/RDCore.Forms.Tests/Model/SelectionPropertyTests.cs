using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using RDCore.Forms.Xaml;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// SelStart, SelLength and SelText, which only a running program sets: on the component, as the runtime's host reads and sets them.
[TestClass]
public sealed class SelectionPropertyTests
{
    private static ComponentInstance Box(string text)
    {
        var box = new ComponentInstance(VBClasses.TextBox, "Text1");
        box.Let(P.Text, text);
        return box;
    }

    private static ComponentInstance Combo(ComboBoxStyle style) => new ComponentInstance(VBClasses.ComboBox, "Combo1").Set(P.ComboBoxStyle, style);

    [TestMethod]
    public void TheRunTimeOnlyProperties_AreTheSelections() =>
        CollectionAssert.AreEqual(
            new[] { "ComboBox.SelLength", "ComboBox.SelStart", "ComboBox.SelText", "TextBox.SelLength", "TextBox.SelStart", "TextBox.SelText" },
            VBClasses.All.SelectMany(c => c.Properties.Where(p => p.RunTimeOnly).Select(p => $"{c.ClassName}.{p.Name}")).Order().ToArray());

    // A run-time-only property is neither read-only nor unavailable: the host lets a program read and set it.
    [TestMethod]
    public void ASelection_IsReadAndSetAtRunTime()
    {
        foreach (var property in new PropertyDefinition[] { P.SelStart, P.SelLength, P.SelText })
        {
            Assert.IsFalse(VBClasses.TextBox.IsReadOnlyAtRunTime(property), property.Name);
            Assert.IsFalse(VBClasses.ComboBox.IsNotAvailableAtRunTime(property), property.Name);
        }
    }

    [TestMethod]
    public void ADropdownList_HasNoSelection_ToReadOrSet()
    {
        var list = Combo(ComboBoxStyle.DropdownList);
        var combo = Combo(ComboBoxStyle.DropdownCombo);

        Assert.IsFalse(list.TryGet(P.SelStart, out _));
        Assert.IsFalse(list.TrySet(P.SelLength, 0));
        Assert.IsFalse(list.TrySet(P.SelText, ""));
        Assert.IsTrue(combo.TryGet(P.SelText, out var text));
        Assert.AreEqual("", text);
        Assert.IsTrue(combo.TrySet(P.SelStart, 0));
    }

    // Each step's text, selection and caret are stored before any of their PropertyChanged: a view's Change, raised from the text,
    // reads the caret after what the step inserted.
    [TestMethod]
    public void ASelTextStep_IsStoredWhole_BeforeItsChangesAreRaised()
    {
        var box = Box("Hello world");
        box.Let(P.SelStart, 2);
        box.Let(P.SelLength, 3);
        var raised = new List<string>();
        box.PropertyChanged += (_, e) =>
            raised.Add($"{e.Property.Name} by {e.AssignedProperty.Name}: {box.Get(P.Text)}@{box.Get(P.SelStart)}+{box.Get(P.SelLength)}");

        box.Let(P.SelText, "XY");

        CollectionAssert.AreEqual(new[]
        {
            "Text by SelText: HeXY world@4+0", "SelStart by SelText: HeXY world@4+0", "SelLength by SelText: HeXY world@4+0",
        }, raised);
    }

    [TestMethod]
    public void ATextFromCode_SelectsNothing_AndTheSameTextKeepsTheSelection()
    {
        var box = Box("Hello world");
        box.Let(P.SelStart, 2);
        box.Let(P.SelLength, 3);
        var raised = new List<string>();
        box.PropertyChanged += (_, e) => raised.Add($"{e.Property.Name} by {e.AssignedProperty.Name}");

        box.Let(P.Text, "Hello world");
        var kept = (box.Get(P.SelStart), box.Get(P.SelLength));
        box.Let(P.Text, "abc");

        Assert.AreEqual((2, 3), kept);
        Assert.AreEqual((0, 0), (box.Get(P.SelStart), box.Get(P.SelLength)));
        CollectionAssert.AreEqual(new[] { "Text by Text", "Text by Text", "SelStart by Text", "SelLength by Text" }, raised);
    }

    [TestMethod]
    public void ANullSelText_IsRefused() => Assert.ThrowsExactly<ArgumentNullException>(() => Box("abc").TrySet(P.SelText, null));

    // A designer's reset of the text leaves no selection past its end to read.
    [TestMethod]
    public void ASelection_IsReadWithinTheText()
    {
        var box = Box("Hello world");
        box.Let(P.SelStart, 6);
        box.Let(P.SelLength, 5);

        box.Reset(P.Text);

        Assert.AreEqual("", box.Get(P.SelText));
    }

    [TestMethod]
    public void AFormsFile_HoldsNoSelection()
    {
        var box = Box("Hello world");
        box.Let(P.SelStart, 2);
        var form = new FormDefinition(new ComponentInstance(VBClasses.Form, "Form1"));
        box.SetContainer(form.Root);

        var written = FormXaml.Write(form);
        var read = FormXaml.Read(written.Replace("Name=\"Text1\"", "Name=\"Text1\" SelLength=\"4\"", StringComparison.Ordinal));
        var text1 = read.Form!.Find("Text1")!;

        Assert.DoesNotContain("SelStart", written);
        Assert.AreEqual(FormXamlDiagnosticKind.UnknownProperty, read.Diagnostics.Single().Kind);
        Assert.IsFalse(text1.IsSet(P.SelLength));
        Assert.AreEqual(new UnmodelledProperty("SelLength", "4"), text1.UnmodelledProperties.Single());
    }

    [TestMethod]
    public void AVB6File_HoldsNoSelection()
    {
        var import = VB6FormReader.Read(string.Join("\r\n",
            "VERSION 5.00",
            "Begin VB.Form Form1 ",
            "   ClientHeight    =   3000",
            "   ClientWidth     =   4000",
            "   Begin VB.TextBox Text1 ",
            "      SelStart        =   3",
            "      Text            =   \"Hello\"",
            "   End",
            "End",
            "Attribute VB_Name = \"Form1\"",
            ""));
        var text1 = import.Form!.Find("Text1")!;

        Assert.IsEmpty(import.Diagnostics);
        Assert.IsFalse(text1.IsSet(P.SelStart));
        Assert.AreEqual(new UnmodelledProperty("SelStart", "3"), text1.UnmodelledProperties.Single());
    }
}

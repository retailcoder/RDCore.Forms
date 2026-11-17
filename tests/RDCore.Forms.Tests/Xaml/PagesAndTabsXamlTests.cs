using RDCore.Forms.Model;
using RDCore.Forms.Xaml;
using K = RDCore.Forms.Xaml.FormXamlDiagnosticKind;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Xaml;

[TestClass]
public sealed class PagesAndTabsXamlTests
{
    private const string Ns = FormXaml.Namespace;

    private static FormXamlRead Read(string body) => FormXaml.Read($"<Form xmlns=\"{Ns}\" FormatVersion=\"1\" Name=\"Form1\">{body}</Form>");

    private static ComponentInstance Placed(ComponentInstance container, ComponentInstance component)
    {
        component.SetContainer(container);
        return component;
    }

    [TestMethod]
    public void AMultiPage_ItsPages_AndTheControlsOnThem_AreWrittenAndReadBack()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        var multiPage = Placed(root, new ComponentInstance(ComponentClasses.MultiPage, "MultiPage1")
            .Set(P.Left, 8d).Set(P.Top, 8d).Set(P.Width, 200d).Set(P.Height, 120d).Set(P.SelectedIndex, 1).Set(P.TabStyle, TabStyle.Buttons)
            .Set(P.TabOrientation, TabOrientation.Left).Set(P.MultiRow, true).Set(P.TabFixedWidth, 60d));
        Placed(multiPage, new ComponentInstance(ComponentClasses.Page, "Page1").Set(P.Caption, "&General"));
        var page2 = Placed(multiPage, new ComponentInstance(ComponentClasses.Page, "Page2").Set(P.Caption, "More").Set(P.ToolTipText, "The rest")
            .Set(P.Enabled, false));
        Placed(page2, new ComponentInstance(ComponentClasses.TextBox, "Text1").Set(P.Left, 4d).Set(P.Text, "x"));
        var written = FormXaml.Write(new FormDefinition(root));

        var read = FormXaml.Read(written);

        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        Assert.AreEqual(written, FormXaml.Write(read.Form!));
        var back = read.Form!.Root.Contents.Single();
        Assert.AreSame(ComponentClasses.MultiPage, back.Class);
        Assert.AreEqual(1, back.Get(P.SelectedIndex));
        Assert.AreEqual(TabStyle.Buttons, back.Get(P.TabStyle));
        Assert.AreEqual(TabOrientation.Left, back.Get(P.TabOrientation));
        Assert.IsTrue(back.Get(P.MultiRow));
        Assert.AreEqual(60d, back.Get(P.TabFixedWidth));
        CollectionAssert.AreEqual(new[] { "&General", "More" }, back.Contents.Select(p => p.Get(P.Caption)).ToArray());
        Assert.IsFalse(back.Contents[1].Get(P.Enabled));
        Assert.AreEqual("Text1", back.Contents[1].Contents.Single().Name);
        StringAssert.Contains(written, "Style=\"Buttons\"");
    }

    [TestMethod]
    public void ATabStripsTabs_AreWrittenAndReadBack_WithWhatIsNotATabsDefault()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        var tabStrip = Placed(root, new ComponentInstance(ComponentClasses.TabStrip, "TabStrip1"));
        tabStrip.Set(P.Tabs, [new Tab("Tab1", "&One"), new Tab("Tab2", "Two") { ToolTipText = "Second", Enabled = false, Visible = false, Tag = "t" }]);
        var written = FormXaml.Write(new FormDefinition(root));

        var read = FormXaml.Read(written);

        Assert.IsEmpty(read.Diagnostics, string.Join(Environment.NewLine, read.Diagnostics));
        Assert.AreEqual(written, FormXaml.Write(read.Form!));
        CollectionAssert.AreEqual(tabStrip.Get(P.Tabs).ToArray(), read.Form!.Root.Contents.Single().Get(P.Tabs).ToArray());
        StringAssert.Contains(written, "<TabStrip.Tabs>");
        StringAssert.Contains(written, "Name=\"Tab1\" Caption=\"&amp;One\"");
    }

    [TestMethod]
    public void ATabStripWithNoTabsWritten_HasTheDefaultTwo_AndOneWithAnEmptyList_HasNone()
    {
        var unwritten = Read("<TabStrip Name=\"TabStrip1\" />").Form!.Root.Contents.Single();
        var empty = Read("<TabStrip Name=\"TabStrip1\"><TabStrip.Tabs /></TabStrip>").Form!.Root.Contents.Single();

        Assert.AreEqual(2, unwritten.Get(P.Tabs).Count);
        Assert.IsFalse(unwritten.IsSet(P.Tabs));
        Assert.AreEqual(0, empty.Get(P.Tabs).Count);
    }

    [TestMethod]
    public void ATabWithNoName_OrAnotherTabsName_OrAFlagThatIsNotOne_IsKeptAsText()
    {
        var read = Read("<TabStrip Name=\"TabStrip1\"><TabStrip.Tabs><Tab Name=\"A\" Caption=\"a\" Enabled=\"maybe\" /><Tab Caption=\"b\" />"
                        + "<Tab Name=\"a\" Caption=\"c\" /></TabStrip.Tabs></TabStrip>");
        var tabStrip = read.Form!.Root.Contents.Single();

        Assert.AreEqual(new Tab("A", "a"), tabStrip.Get(P.Tabs).Single());
        CollectionAssert.AreEqual(new[] { K.InvalidValue, K.UnmodelledElement, K.UnmodelledElement }, read.Diagnostics.Select(d => d.Kind).ToArray());
        Assert.AreEqual("Tabs(0).Enabled", tabStrip.UnmodelledProperties.Single().Name);
    }

    [TestMethod]
    public void APageOutsideAMultiPage_IsKeptAsText_AndAControlInAMultiPage_GoesOnItsForm()
    {
        var read = Read("<Page Name=\"Page1\"><TextBox Name=\"Text1\" /></Page>"
                        + "<MultiPage Name=\"MultiPage1\"><TextBox Name=\"Text2\" /><Page Name=\"Page2\" /></MultiPage>");
        var root = read.Form!.Root;

        CollectionAssert.AreEqual(new[] { K.UnplaceableComponent, K.MisplacedComponent }, read.Diagnostics.Select(d => d.Kind).ToArray());
        Assert.AreEqual("Page1", root.UnmodelledContents.Single().Name);
        CollectionAssert.AreEqual(new[] { "MultiPage1", "Text2" }, root.Contents.Select(c => c.Name).ToArray());
        Assert.AreEqual("Page2", root.Contents[0].Contents.Single().Name);
    }
}

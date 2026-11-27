using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class PagesAndTabsTests
{
    private static FormDefinition Form() => new(new ComponentInstance(ComponentClasses.Form, "Form1"));

    // A MultiPage a program added, with the two pages it comes with.
    private static (FormDefinition Form, ComponentInstance MultiPage, Pages Pages) MultiPage()
    {
        var form = Form();
        var multiPage = form.AddControl(ComponentClasses.MultiPage, "MultiPage1");
        return (form, multiPage, Pages.Of(form, multiPage));
    }

    private static int CodeOf(Action action) => Assert.ThrowsExactly<FormErrorException>(action).Error.Number;

    [TestMethod]
    [DataRow("RDForms.MultiPage")]
    [DataRow("RDForms.Page")]
    [DataRow("RDForms.TabStrip")]
    public void TheCatalog_HasTheClass(string typeName) => Assert.IsNotNull(ComponentClasses.ByTypeName(typeName));

    [TestMethod]
    public void AMultiPageAndATabStrip_DefaultToTheirFirstTab_ShownAsTabsAlongTheTop_InOneRow()
    {
        foreach (var type in new[] { ComponentClasses.MultiPage, ComponentClasses.TabStrip })
        {
            Assert.AreEqual(0, type.DefaultOf(P.SelectedIndex), type.TypeName);
            Assert.AreEqual(TabStyle.Tabs, type.DefaultOf(P.TabStyle), type.TypeName);
            Assert.AreEqual(TabOrientation.Top, type.DefaultOf(P.TabOrientation), type.TypeName);
            Assert.IsFalse(type.DefaultOf(P.MultiRow), type.TypeName);
            Assert.AreEqual(0d, type.DefaultOf(P.TabFixedWidth), type.TypeName);
            Assert.AreEqual(0d, type.DefaultOf(P.TabFixedHeight), type.TypeName);
            Assert.AreEqual("Value", type.DefaultMember, type.TypeName);
            Assert.AreSame(P.SelectedIndex, type.Property("Value"), type.TypeName);
            Assert.AreSame(P.TabStyle, type.Property("Style"), type.TypeName);
        }
    }

    [TestMethod]
    public void ATabStrip_HasTwoTabs_Tab1AndTab2_ByDefault()
    {
        var tabs = Tabs.Of(new ComponentInstance(ComponentClasses.TabStrip, "TabStrip1"));

        CollectionAssert.AreEqual(new[] { "Tab1", "Tab2" }, tabs.Select(t => t.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "Tab1", "Tab2" }, tabs.Select(t => t.Caption).ToArray());
        Assert.IsTrue(tabs.All(t => t is { Enabled: true, Visible: true, ToolTipText: "", Tag: "" }));
    }

    [TestMethod]
    public void TheEvents_NameTheTabTheyAreAboutFirst()
    {
        foreach (var type in new[] { ComponentClasses.MultiPage, ComponentClasses.TabStrip })
        {
            foreach (var name in new[] { "Click", "DblClick", "MouseDown", "MouseMove", "MouseUp" })
                Assert.AreEqual(new EventParameter("Index", "Long"), type.Event(name)!.Parameters[0], $"{type.TypeName}.{name}");
            Assert.AreEqual(0, type.Event("Change")!.Parameters.Count);
        }

        Assert.AreEqual(new EventParameter("Cancel", "Boolean"), ComponentClasses.MultiPage.Event("DblClick")!.Parameters[1]);
        Assert.AreEqual(2, ComponentClasses.MultiPage.Event("AddControl")!.Parameters.Count);
        Assert.IsNull(ComponentClasses.TabStrip.Event("Layout"));
        Assert.AreEqual(0, ComponentClasses.Page.Events.Count);
    }

    [TestMethod]
    public void AValue_BelowMinusOne_IsRefused()
    {
        Assert.IsTrue(P.SelectedIndex.Accepts(-1));
        Assert.IsFalse(P.SelectedIndex.Accepts(-2));
        Assert.IsFalse(P.TabFixedWidth.Accepts(-1d));
        Assert.IsFalse(P.TabStyle.Accepts((TabStyle)3));
    }

    // As measured in Excel: a program sets a MultiPage's or a TabStrip's Value to -1 or to one of its pages or tabs; any other value is 380,
    // and the selection stays. With every page or tab removed, only -1 is left.
    [TestMethod]
    public void AProgramsValue_IsMinusOneOrAPageOrATab_AndAnyOtherIs380_TheSelectionStaying()
    {
        var (_, multiPage, pages) = MultiPage();
        var tabStrip = new ComponentInstance(ComponentClasses.TabStrip, "TabStrip1");
        foreach (var control in new[] { multiPage, tabStrip })
        {
            var codes = new List<int>();
            var values = new List<int>();
            foreach (var value in new[] { 1, 0, 2, -1, 5, 1 })
            {
                codes.Add(Code(() => RunTime.Assign(control, P.SelectedIndex, value, onAVisualBasic6Form: false)));
                values.Add(control.Get(P.SelectedIndex));
            }
            CollectionAssert.AreEqual(new[] { 0, 0, 380, 0, 380, 0 }, codes, control.Name);
            CollectionAssert.AreEqual(new[] { 1, 0, 0, -1, -1, 1 }, values, control.Name);
            Assert.IsFalse(control.TrySet(P.SelectedIndex, 2), control.Name);
        }

        pages.Clear();
        Tabs.Of(tabStrip).Clear();
        foreach (var control in new[] { multiPage, tabStrip })
            CollectionAssert.AreEqual(new[] { 380, 0 },
                new[] { Code(() => RunTime.Assign(control, P.SelectedIndex, 0, onAVisualBasic6Form: false)),
                        Code(() => RunTime.Assign(control, P.SelectedIndex, -1, onAVisualBasic6Form: false)) }, control.Name);
    }

    // The error an action raises, or 0 where it raises none.
    private static int Code(Action action)
    {
        try
        {
            action();
            return 0;
        }
        catch (FormErrorException e)
        {
            return e.Error.Number;
        }
    }

    [TestMethod]
    public void TabsOfTheSameName_AreRefused_WithoutRegardToCase() =>
        Assert.IsFalse(P.Tabs.Accepts(new List<Tab> { new("Tab1", "A"), new("TAB1", "B") }));

    // Placement

    [TestMethod]
    public void APage_GoesInAMultiPageOnly_AndAMultiPage_HoldsPagesOnly()
    {
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");
        var frame = new ComponentInstance(ComponentClasses.Frame, "Frame1");
        var multiPage = new ComponentInstance(ComponentClasses.MultiPage, "MultiPage1");
        var page = new ComponentInstance(ComponentClasses.Page, "Page1");

        Assert.ThrowsExactly<InvalidOperationException>(() => page.SetContainer(form));
        Assert.ThrowsExactly<InvalidOperationException>(() => page.SetContainer(frame));
        Assert.ThrowsExactly<InvalidOperationException>(() => new ComponentInstance(ComponentClasses.TextBox, "Text1").SetContainer(multiPage));
        page.SetContainer(multiPage);
        new ComponentInstance(ComponentClasses.TextBox, "Text1").SetContainer(page);
        Assert.IsTrue(ComponentClasses.CanHold(ComponentClasses.Page, ComponentClasses.Frame));
        Assert.IsFalse(ComponentClasses.CanHold(ComponentClasses.CommandButton, ComponentClasses.Page));
    }

    [TestMethod]
    public void AddingAControlToAMultiPage_OrAPageToAForm_Raises721()
    {
        var (form, multiPage, _) = MultiPage();

        Assert.AreEqual(721, CodeOf(() => form.AddControl(ComponentClasses.TextBox, "Text1", multiPage)));
        Assert.AreEqual(721, CodeOf(() => form.AddControl(ComponentClasses.Page, "Page9")));
    }

    // Pages

    [TestMethod]
    public void AMultiPageAProgramAdds_HasTwoShownPages_Page1AndPage2_TheFirstSelected()
    {
        var (_, multiPage, pages) = MultiPage();

        CollectionAssert.AreEqual(new[] { "Page1", "Page2" }, pages.Select(p => p.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "Page1", "Page2" }, pages.Select(p => p.Get(P.Caption)).ToArray());
        Assert.IsTrue(pages.All(p => p.Get(P.Visible)));
        Assert.AreSame(pages[0], pages.SelectedItem);
        Assert.AreEqual(0, multiPage.Get(P.SelectedIndex));
    }

    [TestMethod]
    public void ASecondMultiPage_TakesTheNextFreePageNames()
    {
        var (form, _, _) = MultiPage();
        var second = form.AddControl(ComponentClasses.MultiPage, "MultiPage2");

        CollectionAssert.AreEqual(new[] { "Page3", "Page4" }, second.Contents.Select(p => p.Name).ToArray());
    }

    [TestMethod]
    public void APage_IsFoundByIndex_AndByName_WithoutRegardToCase()
    {
        var (_, _, pages) = MultiPage();

        Assert.AreEqual(2, pages.Count);
        Assert.AreSame(pages[1], pages["PAGE2"]);
        Assert.AreEqual(1, pages.IndexOf(pages[1]));
        Assert.AreEqual(5, CodeOf(() => _ = pages[2]));
        Assert.AreEqual(730, CodeOf(() => _ = pages["Page9"]));
    }

    [TestMethod]
    public void Add_NamesTheNextFreePage_CaptionsItWithItsName_AndPlacesItLast_OrAtItsIndex()
    {
        var (_, _, pages) = MultiPage();

        var last = pages.Add();
        var first = pages.Add("Intro", "&Introduction", 0);

        CollectionAssert.AreEqual(new[] { "Intro", "Page1", "Page2", "Page3" }, pages.Select(p => p.Name).ToArray());
        Assert.AreEqual("Page3", last.Get(P.Caption));
        Assert.AreEqual("&Introduction", first.Get(P.Caption));
        Assert.AreEqual(5, CodeOf(() => pages.Add(index: 9)));
        Assert.AreEqual(727, CodeOf(() => pages.Add("intro")));
    }

    [TestMethod]
    public void TheSelectedPage_StaysSelected_AsPagesAreAddedBeforeIt_AndRemovedBeforeIt()
    {
        var (_, multiPage, pages) = MultiPage();
        multiPage.Set(P.SelectedIndex, 1);
        var selected = pages.SelectedItem;

        pages.Add(index: 0);
        Assert.AreSame(selected, pages.SelectedItem);
        Assert.AreEqual(2, multiPage.Get(P.SelectedIndex));
        pages.Remove(0);
        Assert.AreSame(selected, pages.SelectedItem);
        Assert.AreEqual(1, multiPage.Get(P.SelectedIndex));
    }

    [TestMethod]
    public void RemovingTheSelectedPage_SelectsTheOneThatTakesItsPlace_OrTheLast_AndClearing_SelectsNone()
    {
        var (_, multiPage, pages) = MultiPage();
        pages.Add();
        multiPage.Set(P.SelectedIndex, 1);

        pages.Remove("Page2");
        Assert.AreEqual("Page3", pages.SelectedItem!.Name);
        pages.Remove(1);
        Assert.AreEqual("Page1", pages.SelectedItem!.Name);
        pages.Clear();
        Assert.AreEqual(0, pages.Count);
        Assert.IsNull(pages.SelectedItem);
        Assert.AreEqual(-1, multiPage.Get(P.SelectedIndex));
        pages.Add();
        Assert.AreEqual(0, multiPage.Get(P.SelectedIndex));
    }

    [TestMethod]
    public void Remove_TakesAPagePlacedAtDesignTime_WithTheControlsOnIt_AndRaisesControlRemoved()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        var multiPage = new ComponentInstance(ComponentClasses.MultiPage, "MultiPage1");
        multiPage.SetContainer(root);
        var page = new ComponentInstance(ComponentClasses.Page, "Page1");
        page.SetContainer(multiPage);
        new ComponentInstance(ComponentClasses.TextBox, "Text1").SetContainer(page);
        var form = new FormDefinition(root);
        var removed = new List<string>();
        form.ControlRemoved += (_, e) => removed.Add(e.Control.Name);

        Pages.Of(form, multiPage).Remove(0);

        Assert.AreEqual(0, multiPage.Contents.Count);
        Assert.IsFalse(root.Descendants().Any(c => c.Name == "Text1"));
        CollectionAssert.AreEqual(new[] { "Page1" }, removed);
    }

    [TestMethod]
    public void PagesOfAnotherClass_OrOfAnotherForm_AreRefused()
    {
        var (form, _, _) = MultiPage();

        Assert.ThrowsExactly<ArgumentException>(() => Pages.Of(form, form.AddControl(ComponentClasses.Frame, "Frame1")));
        Assert.ThrowsExactly<ArgumentException>(() => Pages.Of(Form(), new ComponentInstance(ComponentClasses.MultiPage, "MultiPage9")));
        Assert.ThrowsExactly<ArgumentException>(() => Tabs.Of(new ComponentInstance(ComponentClasses.Frame, "Frame1")));
    }

    // Tabs

    [TestMethod]
    public void Tabs_AddFindReplaceAndRemove_AndTheValueFollowsTheSelectedTab()
    {
        var tabStrip = new ComponentInstance(ComponentClasses.TabStrip, "TabStrip1").Set(P.SelectedIndex, 1);
        var tabs = Tabs.Of(tabStrip);

        var added = tabs.Add();
        tabs.Add("First", "&First", 0);
        tabs.Replace(1, tabs[1] with { Caption = "One", Enabled = false });

        CollectionAssert.AreEqual(new[] { "First", "Tab1", "Tab2", "Tab3" }, tabs.Select(t => t.Name).ToArray());
        Assert.AreEqual(new Tab("Tab3", "Tab3"), added);
        Assert.AreEqual("One", tabs["tab1"].Caption);
        Assert.IsFalse(tabs[1].Enabled);
        Assert.AreEqual("Tab2", tabs.SelectedItem!.Name);
        tabs.Remove("First");
        Assert.AreEqual("Tab2", tabs.SelectedItem!.Name);
        tabs.Remove(1);
        Assert.AreEqual("Tab3", tabs.SelectedItem!.Name);
        tabs.Clear();
        Assert.IsNull(tabs.SelectedItem);
        Assert.AreEqual(-1, tabStrip.Get(P.SelectedIndex));
    }

    [TestMethod]
    public void ATabOfAnotherTabsName_OrAnIndexPastTheTabs_Raises5()
    {
        var tabs = Tabs.Of(new ComponentInstance(ComponentClasses.TabStrip, "TabStrip1"));

        Assert.AreEqual(5, CodeOf(() => tabs.Add("TAB2")));
        Assert.AreEqual(5, CodeOf(() => tabs.Add(index: 3)));
        Assert.AreEqual(5, CodeOf(() => tabs.Replace(0, new Tab("Tab2", "x"))));
        Assert.AreEqual(5, CodeOf(() => _ = tabs[2]));
        Assert.AreEqual(5, CodeOf(() => _ = tabs["Tab9"]));
        Assert.AreEqual(5, CodeOf(() => tabs.Remove("Tab9")));
    }

    [TestMethod]
    public void AProgramsContainer_PutsAPageInAMultiPageOnly_AndAControlInAPageButNotInAMultiPage()
    {
        var (form, multiPage, pages) = MultiPage();
        var other = form.AddControl(ComponentClasses.MultiPage, "MultiPage2");
        var frame = form.AddControl(ComponentClasses.Frame, "Frame1");
        var text = form.AddControl(ComponentClasses.TextBox, "Text1");
        var (page1, page2) = (pages[0], pages[1]);

        Assert.AreEqual(425, CodeOf(() => form.SetContainer(page1, frame)));
        Assert.AreEqual(425, CodeOf(() => form.SetContainer(page1, form.Root)));
        Assert.AreEqual(425, CodeOf(() => form.SetContainer(text, multiPage)));
        Assert.AreSame(multiPage, page1.Container);
        Assert.AreSame(form.Root, text.Container);

        form.SetContainer(text, page2);
        form.SetContainer(page1, other);

        Assert.AreSame(page2, text.Container);
        Assert.AreSame(other, page1.Container);
        CollectionAssert.AreEqual(new[] { page2 }, multiPage.Contents.ToArray());
    }
}

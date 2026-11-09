using RDCore.Forms.Import.VB6;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class FormMenuTests
{
    // The form of VB6's measurements: a Game menu holding New on F2, a separator, and three levels, the first ticked.
    private const string Game = """
        VERSION 5.00
        Begin VB.Form Form1
           Caption         =   "Probe"
           Begin VB.Menu mnuGame
              Caption         =   "&Game"
              Begin VB.Menu mnuNew
                 Caption         =   "&New"
                 Shortcut        =   {F2}
              End
              Begin VB.Menu mnuSep1
                 Caption         =   "-"
              End
              Begin VB.Menu mnuBeginner
                 Caption         =   "&Beginner"
                 Checked         =   -1  'True
              End
              Begin VB.Menu mnuIntermediate
                 Caption         =   "&Intermediate"
              End
              Begin VB.Menu mnuExpert
                 Caption         =   "&Expert"
              End
           End
        End
        Attribute VB_Name = "Form1"
        """;

    private static FormMenu Read() => FormMenu.Of(VB6FormReader.Read(Game.ReplaceLineEndings("\r\n")).Form!.Root);

    private static FormMenuItem Item(FormMenu menu, string name) => menu.AllItems.Single(item => item.Component.Name == name);

    private static int Refused(Action set) => Assert.ThrowsExactly<FormErrorException>(set).Error.Number;

    [TestMethod]
    [DataRow("^Q", (short)81, ShortcutModifiers.Ctrl, "^Q")]
    [DataRow("{F2}", (short)113, ShortcutModifiers.None, "{F2}")]
    [DataRow("^{insert}", (short)45, ShortcutModifiers.Ctrl, "^{INSERT}")]
    [DataRow("+^{DELETE}", (short)46, ShortcutModifiers.Ctrl | ShortcutModifiers.Shift, "^+{DEL}")]
    [DataRow("%{BS}", (short)8, ShortcutModifiers.Alt, "%{BKSP}")]
    [DataRow("^5", (short)53, ShortcutModifiers.Ctrl, "^5")]
    [DataRow("^+{F16}", (short)127, ShortcutModifiers.Ctrl | ShortcutModifiers.Shift, "^+{F16}")]
    [DataRow("{PGDN}", (short)34, ShortcutModifiers.None, "{PGDN}")]
    public void AShortcut_IsReadAsVBWritesIt_AndWrittenBack(string text, short keyCode, ShortcutModifiers modifiers, string written)
    {
        Assert.IsTrue(MenuShortcut.TryParse(text, out var shortcut));
        Assert.AreEqual(new MenuShortcut(keyCode, modifiers), shortcut);
        Assert.AreEqual(written, shortcut.ToString());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("^")]
    [DataRow("^QQ")]
    [DataRow("{F17}")]
    [DataRow("{}")]
    [DataRow("^{NOKEY}")]
    [DataRow("^;")]
    public void SomethingThatIsNotAShortcut_IsNotRead(string text) => Assert.IsFalse(MenuShortcut.TryParse(text, out _));

    [TestMethod]
    public void AFormsMenus_AreItsMenuComponents_EachHoldingItsOwnItems()
    {
        var menu = Read();

        Assert.AreEqual("mnuGame", menu.Items.Single().Component.Name);
        CollectionAssert.AreEqual(new[] { "mnuNew", "mnuSep1", "mnuBeginner", "mnuIntermediate", "mnuExpert" },
            menu.Items[0].Items.Select(item => item.Component.Name).ToArray());
        Assert.IsTrue(menu.Items[0].IsTopLevel);
        Assert.AreSame(menu.Items[0], Item(menu, "mnuNew").Parent);
        Assert.IsTrue(Item(menu, "mnuSep1").IsSeparator);
        Assert.AreEqual(new MenuShortcut(113), Item(menu, "mnuNew").Shortcut);
        Assert.IsTrue(Item(menu, "mnuBeginner").Checked);
        Assert.AreSame(menu, FormMenu.Of(menu.Root));
        Assert.AreSame(Item(menu, "mnuNew"), FormMenuItem.Of(Item(menu, "mnuNew").Component));
    }

    [TestMethod]
    public void TheItems_RefuseWhatVB6Refuses()
    {
        var menu = Read();
        Item(menu, "mnuIntermediate").Checked = true;

        Assert.AreEqual(387, Refused(() => Item(menu, "mnuGame").Checked = true));
        Assert.AreEqual(387, Refused(() => Item(menu, "mnuSep1").Checked = true));
        Assert.AreEqual(387, Refused(() => Item(menu, "mnuSep1").Enabled = false));
        Assert.AreEqual(395, Refused(() => Item(menu, "mnuNew").Caption = "-"));
        Assert.IsTrue(Item(menu, "mnuIntermediate").Checked);
        Assert.IsTrue(Item(menu, "mnuSep1").Enabled);
        Assert.AreEqual("&New", Item(menu, "mnuNew").Caption);
    }

    [TestMethod]
    public void HidingTheLastVisibleItemOfAMenu_IsRefused_ASeparatorIncluded()
    {
        var menu = Read();
        Item(menu, "mnuNew").Visible = false;
        Item(menu, "mnuSep1").Visible = false;
        Item(menu, "mnuBeginner").Visible = false;
        Item(menu, "mnuIntermediate").Visible = false;

        Assert.AreEqual(387, Refused(() => Item(menu, "mnuExpert").Visible = false));
        Item(menu, "mnuSep1").Visible = true;
        Item(menu, "mnuExpert").Visible = false;
        Assert.AreEqual(387, Refused(() => Item(menu, "mnuSep1").Visible = false));
    }

    [TestMethod]
    public void HidingEveryItemOfTheBar_LeavesTheFormWithNoBar()
    {
        var menu = Read();
        Assert.IsTrue(menu.ShowsTheBar);

        menu.Items[0].Visible = false;

        Assert.IsFalse(menu.ShowsTheBar);
    }

    [TestMethod]
    public void AMenuOfAFormRead_KeepsAShortcutItCannotRead_AsAnUnmodelledValue()
    {
        var import = VB6FormReader.Read(Game.Replace("{F2}", "{F99}").ReplaceLineEndings("\r\n"));
        var item = Item(FormMenu.Of(import.Form!.Root), "mnuNew");

        Assert.IsNull(item.Shortcut);
        Assert.IsNotEmpty(import.Diagnostics);
    }

    [TestMethod]
    public void AComponentThatIsNotAMenu_IsRefused()
    {
        Assert.ThrowsExactly<ArgumentException>(() => FormMenuItem.Of(new ComponentInstance(VBClasses.Label, "Label1")));
        Assert.ThrowsExactly<ArgumentException>(() => FormMenu.Of(new ComponentInstance(VBClasses.Menu, "mnu1")));
        Assert.IsFalse(new ComponentInstance(VBClasses.Menu, "mnu1").Get(P.CheckOnClick));
    }
}

using RDCore.Forms.Import.VB6;
using RDCore.Forms.Menus;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// Menu arrays at run time, as measured with VB6 on this form.
[TestClass]
public sealed class FormMenuArrayTests
{
    private const string Probe = """
        VERSION 5.00
        Begin VB.Form Form1
           Caption         =   "Form1"
           Begin VB.Menu mnuFile
              Caption         =   "&File"
              Begin VB.Menu mnuOpen
                 Caption         =   "&Open"
                 HelpContextID   =   4
              End
              Begin VB.Menu mnuMRU
                 Caption         =   "M0"
                 Index           =   0
                 Shortcut        =   ^S
              End
              Begin VB.Menu mnuSep
                 Caption         =   "-"
              End
              Begin VB.Menu mnuExit
                 Caption         =   "E&xit"
              End
           End
           Begin VB.Menu mnuEdit
              Caption         =   "&Edit"
              NegotiatePosition=   1  'Left
              Begin VB.Menu mnuEditArr
                 Caption         =   "E1"
                 Index           =   1
              End
              Begin VB.Menu mnuGroup
                 Caption         =   "G0"
                 Index           =   0
                 Begin VB.Menu mnuGroupChild
                    Caption         =   "GroupChild"
                 End
              End
              Begin VB.Menu mnuEditAfter
                 Caption         =   "EditAfter"
              End
           End
           Begin VB.Menu mnuTwoMenu
              Caption         =   "Two"
              Begin VB.Menu mnuTwo
                 Caption         =   "T0"
                 Index           =   0
              End
              Begin VB.Menu mnuOther
                 Caption         =   "Other"
              End
              Begin VB.Menu mnuTwo
                 Caption         =   "T2"
                 Index           =   2
              End
              Begin VB.Menu mnuLast
                 Caption         =   "Last"
              End
           End
           Begin VB.Menu mnuSolo
              Caption         =   "Solo"
              Begin VB.Menu mnuSoloArr
                 Caption         =   "S0"
                 Index           =   0
              End
           End
           Begin VB.Menu mnuTop
              Caption         =   "Top0"
              Index           =   0
           End
        End
        Attribute VB_Name = "Form1"
        """;

    private static FormMenu Read() => FormMenu.Of(VB6FormReader.Read(Probe.ReplaceLineEndings("\r\n")).Form!.Root);

    private static FormMenuItem Item(FormMenu menu, string name) => menu.AllItems.First(item => item.Component.Name == name);

    private static FormMenuArray Array(FormMenu menu, string name) => menu.ArrayOf(name)!;

    private static string[] Captions(FormMenu menu, string name) => [.. Item(menu, name).Items.Select(item => item.Caption)];

    private static void Load(FormMenuArray array, int index, string caption) => array.Load(index).Caption = caption;

    private static int Refused(Action action) => Assert.ThrowsExactly<FormErrorException>(action).Error.Number;

    [TestMethod]
    public void ALoadedElement_GoesAmongItsArray_InIndexOrder()
    {
        var menu = Read();
        var mru = Array(menu, "mnuMRU");
        Load(mru, 1, "M1");
        Load(mru, 3, "M3");
        Load(mru, 2, "M2");
        Load(mru, 5, "M5");
        Load(mru, 4, "M4");
        var file = Captions(menu, "mnuFile");
        mru.Unload(3);
        Load(mru, 3, "N3");

        CollectionAssert.AreEqual(new[] { "&Open", "M0", "M1", "M2", "M3", "M4", "M5", "-", "E&xit" }, file);
        CollectionAssert.AreEqual(new[] { "&Open", "M0", "M1", "M2", "N3", "M4", "M5", "-", "E&xit" }, Captions(menu, "mnuFile"));
    }

    [TestMethod]
    public void AnElementWithNoneBelowIt_GoesBeforeTheLowestAboveIt_AndOthersStayOnTheirSide()
    {
        var menu = Read();
        Load(Array(menu, "mnuEditArr"), 0, "E0");
        var two = Array(menu, "mnuTwo");
        Load(two, 1, "T1");
        Load(two, 3, "T3");
        Load(Array(menu, "mnuTop"), 1, "Top1");

        CollectionAssert.AreEqual(new[] { "E0", "E1", "G0", "EditAfter" }, Captions(menu, "mnuEdit"));
        CollectionAssert.AreEqual(new[] { "T0", "T1", "Other", "T2", "T3", "Last" }, Captions(menu, "mnuTwoMenu"));
        CollectionAssert.AreEqual(new[] { "&File", "&Edit", "Two", "Solo", "Top0", "Top1" }, menu.Items.Select(item => item.Caption).ToArray());
    }

    [TestMethod]
    public void ALoadedElement_CopiesTheLowestIndex_ButNotItsShortcutNorItsMenu()
    {
        var menu = Read();
        var zero = Item(menu, "mnuMRU");
        zero.Checked = true;
        zero.Caption = "Zero";
        zero.Component.Set(P.Tag, "t0");
        zero.HelpContextID = 7;
        zero.Enabled = false;
        zero.CheckOnClick = true;
        zero.Picture = [1, 2, 3];
        var one = Array(menu, "mnuMRU").Load(1);
        var group = Array(menu, "mnuGroup").Load(1);

        Assert.AreEqual(("Zero", true, false, true, "t0", 7, 1), (one.Caption, one.Checked, one.Enabled, one.Visible, one.Component.Get(P.Tag), one.HelpContextID, one.Index));
        Assert.IsNull(one.Shortcut);
        Assert.IsTrue(one.CheckOnClick);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, one.Picture);
        Assert.AreEqual(("G0", 0), (group.Caption, group.Items.Count));
    }

    [TestMethod]
    public void ALoad_CopiesTheLowestIndexLoadedAtTheTime_AHiddenOneIncluded()
    {
        var menu = Read();
        var edit = Array(menu, "mnuEditArr");
        var e0 = edit.Load(0);
        e0.Caption = "E0";
        e0.Checked = true;
        var five = edit.Load(5);
        edit[1].Caption = "X1";
        edit.Unload(0);
        var three = edit.Load(3);
        var mru = Array(menu, "mnuMRU");
        mru[0].Visible = false;
        var hidden = mru.Load(1);

        Assert.AreEqual(("E0", true), (five.Caption, five.Checked));
        Assert.AreEqual(("X1", false), (three.Caption, three.Checked));
        Assert.IsFalse(hidden.Visible);
    }

    [TestMethod]
    public void TheArraysMembers_CountTheElementsLoaded_InIndexOrder()
    {
        var menu = Read();
        var mru = Array(menu, "mnuMRU");
        for (var index = 1; index <= 3; index++)
            mru.Load(index);
        mru.Unload(2);
        var after = (mru.Count, mru.LBound, mru.UBound);
        mru.Load(32767);
        mru.Load(5);
        mru.Load(4);

        Assert.AreEqual((3, 0, 3), after);
        Assert.AreEqual(32767, mru.UBound);
        CollectionAssert.AreEqual(new int?[] { 0, 1, 3, 4, 5, 32767 }, mru.Select(element => element.Index).ToArray());
    }

    // VB6 reads a file that lists an array's elements out of index order: they are shown in the file's order, and the members go in
    // index order.
    [TestMethod]
    public void AFileThatListsTheElementsOutOfOrder_ShowsThemInItsOrder_AndTheMembersGoInIndexOrder()
    {
        const string outOfOrder = """
            VERSION 5.00
            Begin VB.Form Form1
               Caption         =   "Form1"
               Begin VB.Menu mnuFile
                  Caption         =   "&File"
                  Begin VB.Menu mnuArr
                     Caption         =   "A2"
                     Index           =   2
                  End
                  Begin VB.Menu mnuArr
                     Caption         =   "A0"
                     Index           =   0
                  End
               End
            End
            Attribute VB_Name = "Form1"
            """;
        var menu = FormMenu.Of(VB6FormReader.Read(outOfOrder.ReplaceLineEndings("\r\n")).Form!.Root);
        var array = Array(menu, "mnuArr");
        var read = (array.LBound, array.UBound, Captions: array.Select(element => element.Caption).ToArray());
        Load(array, 1, "A1");
        Load(array, 3, "A3");

        Assert.AreEqual((0, 2), (read.LBound, read.UBound));
        CollectionAssert.AreEqual(new[] { "A0", "A2" }, read.Captions);
        CollectionAssert.AreEqual(new[] { "A2", "A3", "A0", "A1" }, Captions(menu, "mnuFile"));
        CollectionAssert.AreEqual(new[] { "A0", "A1", "A2", "A3" }, array.Select(element => element.Caption).ToArray());
    }

    [TestMethod]
    public void EachRefusal_IsVB6sError_AndChangesNothing()
    {
        var menu = Read();
        var mru = Array(menu, "mnuMRU");
        mru.Load(1);
        var solo = Array(menu, "mnuSoloArr");
        solo.Load(1);
        solo[0].Visible = false;
        var before = Captions(menu, "mnuFile");

        Assert.AreEqual(360, Refused(() => mru.Load(1)));
        Assert.AreEqual(362, Refused(() => mru.Unload(0)));
        Assert.AreEqual(340, Refused(() => mru.Unload(2)));
        Assert.AreEqual(340, Refused(() => _ = mru[2]));
        Assert.AreEqual(341, Refused(() => mru.Load(-1)));
        Assert.AreEqual(6, Refused(() => mru.Load(32768)));
        Assert.AreEqual(361, Refused(() => solo.Unload(1)));
        CollectionAssert.AreEqual(before, Captions(menu, "mnuFile"));
        Assert.AreEqual((2, true), (mru.Count, solo[1].Visible));
    }

    [TestMethod]
    public void TheLastVisibleItemOfTheBar_CanBeUnloaded()
    {
        var menu = Read();
        var top = Array(menu, "mnuTop");
        top.Load(1);
        foreach (var item in menu.Items.Where(item => item.Index != 1))
            item.Visible = false;
        top.Unload(1);

        Assert.IsFalse(menu.ShowsTheBar);
    }

    [TestMethod]
    public void AnItemsOwnLoadAndUnload_AreAnElementsOrError361()
    {
        var menu = Read();
        var plain = Item(menu, "mnuOpen");
        var mru = Array(menu, "mnuMRU");
        var one = mru.Load(1);

        Assert.AreEqual(361, Refused(plain.Load));
        Assert.AreEqual(361, Refused(plain.Unload));
        Assert.AreEqual(360, Refused(one.Load));
        one.Unload();
        Assert.AreEqual(340, Refused(one.Load));
        Assert.AreEqual(340, Refused(one.Unload));
        Assert.IsFalse(mru.IsLoaded(1));
    }

    [TestMethod]
    public void TheFormsArrays_AreItsIndexedNames()
    {
        var menu = Read();

        CollectionAssert.AreEqual(new[] { "mnuMRU", "mnuEditArr", "mnuGroup", "mnuTwo", "mnuSoloArr", "mnuTop" },
            menu.Arrays.Select(array => array.Name).ToArray());
        Assert.IsNull(menu.ArrayOf("mnuOpen"));
        Assert.AreSame(menu.ArrayOf("mnuMRU"), menu.ArrayOf("MNUMRU"));
        Assert.AreSame(menu.ArrayOf("mnuMRU"), Item(menu, "mnuMRU").Array);
        Assert.IsNull(Item(menu, "mnuOpen").Array);
    }

    [TestMethod]
    public void LoadAndUnload_RaiseTheirEvents_WithTheContainer()
    {
        var menu = Read();
        var raised = new List<string>();
        menu.ItemLoaded += (_, e) => raised.Add($"loaded {e.Item.Index} in {e.Container.Name}");
        menu.ItemUnloaded += (_, e) => raised.Add($"unloaded {e.Item.Index} from {e.Container.Name}");
        var one = Array(menu, "mnuMRU").Load(1);
        var loaded = menu.IsLoadedAtRunTime(one);
        one.Unload();

        CollectionAssert.AreEqual(new[] { "loaded 1 in mnuFile", "unloaded 1 from mnuFile" }, raised);
        Assert.IsTrue(loaded);
        Assert.IsFalse(menu.IsLoadedAtRunTime(one));
        Assert.IsFalse(menu.IsLoadedAtRunTime(Item(menu, "mnuMRU")));
    }

    [TestMethod]
    public void AMenusHelpContextID_AndNegotiatePosition_AreReadFromItsFile_AndNegotiatePositionIsNotAvailableAtRunTime()
    {
        var menu = Read();
        var open = Item(menu, "mnuOpen");
        open.HelpContextID = -5;

        Assert.AreEqual(-5, open.HelpContextID);
        Assert.AreEqual(4, Read() is var fresh ? Item(fresh, "mnuOpen").HelpContextID : 0);
        Assert.AreEqual(NegotiatePosition.Left, Item(menu, "mnuEdit").Component.Get(P.NegotiatePosition));
        Assert.IsTrue(ComponentClasses.Menu.IsNotAvailableAtRunTime(P.NegotiatePosition));
        Assert.IsFalse(ComponentClasses.Menu.IsReadOnlyAtRunTime(P.HelpContextID));
    }
}

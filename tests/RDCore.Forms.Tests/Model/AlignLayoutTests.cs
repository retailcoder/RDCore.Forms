using System.Globalization;
using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// Aligned PictureBoxes, as measured with VB6 on these forms, native and p-code agreeing. The file positions of the aligned boxes are
// not their laid-out ones. Sizes are written in twips, as VB6 reads them.
[TestClass]
public sealed class AlignLayoutTests
{
    private const double TwipsPerPixel = 15;

    private const string Probe = """
        VERSION 5.00
        Begin VB.Form Form1
           Caption         =   "Form1"
           ClientHeight    =   4500
           ClientLeft      =   60
           ClientTop       =   345
           ClientWidth     =   6000
           ScaleHeight     =   4500
           ScaleWidth      =   6000
           Begin VB.PictureBox PicL
              Align           =   3  'Align Left
              Height          =   1000
              Left            =   300
              Top             =   300
              Width           =   900
           End
           Begin VB.PictureBox PicT1
              Align           =   1  'Align Top
              Height          =   600
              Left            =   500
              Top             =   2000
              Width           =   2000
           End
           Begin VB.PictureBox PicB
              Align           =   2  'Align Bottom
              Height          =   500
              Left            =   0
              Top             =   0
              Width           =   6000
           End
           Begin VB.PictureBox PicT2
              Align           =   1  'Align Top
              Height          =   400
              Left            =   0
              Top             =   0
              Width           =   6000
           End
           Begin VB.PictureBox PicR
              Align           =   4  'Align Right
              Height          =   4500
              Left            =   0
              Top             =   0
              Width           =   700
           End
           Begin VB.PictureBox PicP
              Height          =   500
              Left            =   2000
              Top             =   1500
              Width           =   1000
           End
           Begin VB.Frame Frame1
              Caption         =   "Frame1"
              Height          =   1200
              Left            =   3500
              Top             =   2500
              Width           =   1500
              Begin VB.PictureBox PicF
                 Height          =   400
                 Left            =   100
                 Top             =   300
                 Width           =   600
              End
           End
        End
        Attribute VB_Name = "Form1"
        """;

    private const string Mdi = """
        VERSION 5.00
        Begin VB.MDIForm MDIForm1
           Caption         =   "MDIForm1"
           ClientHeight    =   4500
           ClientLeft      =   60
           ClientTop       =   345
           ClientWidth     =   6000
           Begin VB.PictureBox PicM1
              Height          =   600
              Left            =   0
              Top             =   0
              Width           =   6000
           End
           Begin VB.PictureBox PicM2
              Align           =   2  'Align Bottom
              Height          =   600
              Left            =   0
              Top             =   3900
              Width           =   6000
           End
        End
        Attribute VB_Name = "MDIForm1"
        """;

    private const string InAFrame = """
        VERSION 5.00
        Begin VB.Form Form1
           Caption         =   "Form1"
           ClientHeight    =   3000
           ClientWidth     =   4000
           Begin VB.Frame Frame1
              Caption         =   "Frame1"
              Height          =   2000
              Left            =   500
              Top             =   500
              Width           =   3000
              Begin VB.PictureBox PicF
                 Align           =   1  'Align Top
                 Height          =   400
                 Left            =   100
                 Top             =   300
                 Width           =   600
              End
           End
        End
        Attribute VB_Name = "Form1"
        """;

    private static FormDefinition Read(string file) => VB6FormReader.Read(file.ReplaceLineEndings("\r\n")).Form!;

    private static AlignLayout Laid(string file) => AlignLayout.Of(Read(file));

    private static ComponentInstance Box(AlignLayout layout, string name) => layout.Form.Components.First(c => c.Name == name);

    private static string Twips(double pixels) => Math.Round(pixels * TwipsPerPixel, 0).ToString(CultureInfo.InvariantCulture);

    // A box's place, as VB6's probe logs it: left,top widthxheight, in twips.
    private static string At(AlignLayout layout, string name)
    {
        var box = Box(layout, name);
        return $"{Twips(box.Get(P.Left))},{Twips(box.Get(P.Top))} {Twips(box.Get(P.Width))}x{Twips(box.Get(P.Height))}";
    }

    private static string[] All(AlignLayout layout) => [.. new[] { "PicL", "PicT1", "PicB", "PicT2", "PicR", "PicP" }.Select(name => $"{name} {At(layout, name)}")];

    private static void Set(AlignLayout layout, string name, PropertyDefinition<double> property, double twips) =>
        Box(layout, name).Set(property, twips / TwipsPerPixel);

    private static void Resize(AlignLayout layout, double width, double height) =>
        layout.Form.Root.Set(P.Width, width / TwipsPerPixel).Set(P.Height, height / TwipsPerPixel);

    private static int Refused(Action action) => Assert.ThrowsExactly<FormErrorException>(action).Error.Number;

    [TestMethod]
    public void TheFilesBoxes_DockFromTheLastIn_TopsAndBottomsAcrossTheForm_LeftsAndRightsBetweenThem()
    {
        var layout = Laid(Probe);

        CollectionAssert.AreEqual(new[]
        {
            "PicL 0,1005 900x3000", "PicT1 0,405 6000x600", "PicB 0,4005 6000x500", "PicT2 0,0 6000x400", "PicR 5295,1005 700x3000",
            "PicP 2000,1500 1000x500",
        }, All(layout));
        CollectionAssert.AreEqual(new[] { "PicR", "PicT2", "PicB", "PicT1", "PicL" }, layout.Docked.Select(box => box.Name).ToArray());
        Assert.AreSame(layout, AlignLayout.Of(layout.Form));
    }

    // A Data control and an Alignable UserControl dock as a box does. One that is also InvisibleAtRuntime has no place to dock.
    [TestMethod]
    public void ADataControlAndAnAlignableUserControl_DockAsABoxDoes_AndAnInvisibleOneDoesNot()
    {
        static ComponentClass Placed(string name, bool invisible) => UserControlClasses.Of(new FormDefinition(
            new ComponentInstance(ComponentClasses.UserControl, name).Set(P.Alignable, true).Set(P.InvisibleAtRuntime, invisible)), "Project1");
        var root = new ComponentInstance(ComponentClasses.Form, "Form1").Set(P.Width, 400d).Set(P.Height, 300d);
        var data = new ComponentInstance(ComponentClasses.Data, "Data1").Set(P.Left, 50d).Set(P.Top, 50d).Set(P.Width, 100d).Set(P.Height, 20d)
            .Set(P.Align, Align.Top);
        var gauge = new ComponentInstance(Placed("Gauge", false), "Gauge1").Set(P.Left, 50d).Set(P.Top, 90d).Set(P.Width, 60d)
            .Set(P.Height, 40d).Set(P.Align, Align.Left);
        var hidden = new ComponentInstance(Placed("Hidden", true), "Hidden1").Set(P.Align, Align.Bottom);
        foreach (var control in new[] { data, gauge, hidden })
            control.SetContainer(root);

        var layout = AlignLayout.Of(new FormDefinition(root));

        CollectionAssert.AreEqual(new[] { "Gauge1", "Data1" }, layout.Docked.Select(box => box.Name).ToArray());
        Assert.AreEqual("0,0 400x20", $"{data.Get(P.Left)},{data.Get(P.Top)} {data.Get(P.Width)}x{data.Get(P.Height)}");
        Assert.AreEqual("0,20 60x280", $"{gauge.Get(P.Left)},{gauge.Get(P.Top)} {gauge.Get(P.Width)}x{gauge.Get(P.Height)}");
        Assert.AreEqual(FormError.PropertyCantBeSetOnThisControl("Height").Number, AlignLayout.Refusal(gauge, P.Height)?.Number);
    }

    [TestMethod]
    public void TheBoxes_FollowTheFormsSize()
    {
        var layout = Laid(Probe);
        Resize(layout, 6000, 5505);
        var taller = All(layout);
        Resize(layout, 4500, 5505);

        CollectionAssert.AreEqual(new[]
        {
            "PicL 0,1005 900x4005", "PicT1 0,405 6000x600", "PicB 0,5010 6000x500", "PicT2 0,0 6000x400", "PicR 5295,1005 700x4005",
            "PicP 2000,1500 1000x500",
        }, taller);
        CollectionAssert.AreEqual(new[]
        {
            "PicL 0,1005 900x4005", "PicT1 0,405 4500x600", "PicB 0,5010 4500x500", "PicT2 0,0 4500x400", "PicR 3795,1005 700x4005",
            "PicP 2000,1500 1000x500",
        }, All(layout));
    }

    [TestMethod]
    public void ABoxsOwnSize_MovesTheOthers_AndItsOtherCoordinatesStay()
    {
        var layout = Laid(Probe);
        Resize(layout, 4500, 5505);
        Set(layout, "PicT1", P.Height, 800);
        var taller = All(layout);
        Set(layout, "PicT1", P.Top, 300);
        Set(layout, "PicT1", P.Width, 1000);
        Set(layout, "PicT1", P.Left, 300);
        Set(layout, "PicL", P.Left, 100);
        Set(layout, "PicL", P.Top, 100);
        Set(layout, "PicL", P.Height, 100);
        var kept = All(layout);
        Set(layout, "PicL", P.Width, 1100);

        CollectionAssert.AreEqual(new[]
        {
            "PicL 0,1200 900x3810", "PicT1 0,405 4500x800", "PicB 0,5010 4500x500", "PicT2 0,0 4500x400", "PicR 3795,1200 700x3810",
            "PicP 2000,1500 1000x500",
        }, taller);
        CollectionAssert.AreEqual(taller, kept);
        Assert.AreEqual("0,1200 1100x3810", At(layout, "PicL"));
    }

    [TestMethod]
    public void AProgramsAssignment_RefusesTheExtentsAlongTheEdge_WithError387_AndLeavesTheOtherEdgeWhereItIs()
    {
        var layout = Laid(Probe);
        ComponentInstance top = Box(layout, "PicT1"), left = Box(layout, "PicL"), bottom = Box(layout, "PicB"), right = Box(layout, "PicR");

        CollectionAssert.AreEqual(new[] { 387, 387, 387, 387, 387, 387, 387, 387 }, new[]
        {
            Refused(() => RunTime.Assign(top, P.Width, 10d)), Refused(() => RunTime.Assign(top, P.Left, 10d)),
            Refused(() => RunTime.Assign(bottom, P.Width, 10d)), Refused(() => RunTime.Assign(bottom, P.Left, 10d)),
            Refused(() => RunTime.Assign(left, P.Height, 10d)), Refused(() => RunTime.Assign(left, P.Top, 10d)),
            Refused(() => RunTime.Assign(right, P.Height, 10d)), Refused(() => RunTime.Assign(right, P.Top, 10d)),
        });
        StringAssert.Contains(Assert.ThrowsExactly<FormErrorException>(() => RunTime.Assign(top, P.Width, 10d)).Message, "Width");
        var before = (At(layout, "PicT1"), At(layout, "PicL"));
        RunTime.Assign(top, P.Top, 10d);
        RunTime.Assign(left, P.Left, 10d);
        Assert.AreEqual(before, (At(layout, "PicT1"), At(layout, "PicL")));
        RunTime.Assign(top, P.Height, 800 / TwipsPerPixel);
        Assert.AreEqual(("0,405 6000x800", "0,1200 900x2805"), (At(layout, "PicT1"), At(layout, "PicL")));
        Assert.IsNull(AlignLayout.Refusal(Box(layout, "PicP"), P.Width));
        Assert.IsNull(AlignLayout.Refusal(top, P.Height));
    }

    [TestMethod]
    public void ABottomBoxTallerThanTheForm_GoesUpPastTheTop_AndTheSidesAre0High()
    {
        var layout = Laid(Probe);
        Resize(layout, 4500, 5505);
        Set(layout, "PicB", P.Height, 99999);

        Assert.AreEqual(("0,-94500 4500x99999", "0,1005 900x0", "3795,1005 700x0"), (At(layout, "PicB"), At(layout, "PicL"), At(layout, "PicR")));
    }

    [TestMethod]
    public void AHiddenBox_TakesNoRoom_KeepsItsPlace_AndDocksWhereItWas_AsItIsShown()
    {
        var layout = Laid(Probe);
        Resize(layout, 4500, 5505);
        Box(layout, "PicT2").Set(P.Visible, false);
        var hidden = (At(layout, "PicT2"), At(layout, "PicT1"), At(layout, "PicL"));
        Set(layout, "PicT2", P.Height, 1000);
        Box(layout, "PicT2").Set(P.Visible, true);

        Assert.AreEqual(("0,0 4500x400", "0,0 4500x600", "0,600 900x4410"), hidden);
        Assert.AreEqual(("0,0 4500x1000", "0,1005 4500x600", "0,1605 900x3405"), (At(layout, "PicT2"), At(layout, "PicT1"), At(layout, "PicL")));
    }

    [TestMethod]
    public void ABoxAlignedAtRunTime_DocksInsideTheOthers_AndKeepsItsPlaceAsItIsNoLongerAligned()
    {
        var layout = Laid(Probe);
        Resize(layout, 4500, 5505);
        Set(layout, "PicT2", P.Height, 1000);
        Set(layout, "PicT1", P.Height, 700);
        var box = Box(layout, "PicP");
        var log = new List<string>();
        foreach (var align in new[] { Align.Right, Align.None, Align.Top, Align.Left, Align.None })
        {
            RunTime.Assign(box, P.Align, align);
            log.Add($"{align}: {At(layout, "PicP")} / {At(layout, "PicL")}");
        }

        CollectionAssert.AreEqual(new[]
        {
            "Right: 2790,1710 1000x3300 / 0,1710 900x3300",
            "None: 2790,1710 1000x3300 / 0,1710 900x3300",
            "Top: 0,1710 4500x3300 / 0,5010 900x0",
            "Left: 900,1710 4500x3300 / 0,1710 900x3300",
            "None: 900,1710 4500x3300 / 0,1710 900x3300",
        }, log);
        Assert.AreEqual(380, Refused(() => RunTime.Assign(box, P.Align, (Align)5)));
        Assert.AreEqual(380, Refused(() => RunTime.Assign(box, P.Align, (Align)(-1))));
        CollectionAssert.AreEqual(new[] { "PicR", "PicT2", "PicB", "PicT1", "PicL" }, layout.Docked.Select(docked => docked.Name).ToArray());
    }

    [TestMethod]
    public void AFormTooSmall_StacksTheBottomsFromItsBottom_AndTheSidesAre0High()
    {
        var layout = Laid(Probe);
        Set(layout, "PicT2", P.Height, 1000);
        Set(layout, "PicT1", P.Height, 700);
        Box(layout, "PicT2").Set(P.Visible, false);
        RunTime.Assign(Box(layout, "PicP"), P.Align, Align.Bottom);
        Set(layout, "PicP", P.Height, 3300);
        Resize(layout, 1800, 915);

        CollectionAssert.AreEqual(new[]
        {
            "PicL 0,705 900x0", "PicT1 0,0 1800x700", "PicB 0,420 1800x500", "PicT2 0,0 6000x1000", "PicR 1095,705 700x0",
            "PicP 0,-2880 1800x3300",
        }, All(layout));
    }

    [TestMethod]
    public void ABoxInAFrame_IsNotAligned_WhatItsFileOrAProgramSays()
    {
        var framed = Laid(InAFrame);
        var layout = Laid(Probe);
        var box = Box(layout, "PicF");
        RunTime.Assign(box, P.Align, Align.Top);

        Assert.AreEqual(Align.None, Box(framed, "PicF").Get(P.Align));
        Assert.AreEqual("100,300 600x400", At(framed, "PicF"));
        Assert.AreEqual((Align.None, "100,300 600x400"), (box.Get(P.Align), At(layout, "PicF")));
        Assert.IsEmpty(framed.Docked);
    }

    [TestMethod]
    public void AnMDIFormsBox_IsAlignedToTheTop_UnlessItsFileSaysOtherwise_AndIsNeverNotAligned()
    {
        var layout = Laid(Mdi);
        ComponentInstance one = Box(layout, "PicM1"), two = Box(layout, "PicM2");
        RunTime.Assign(one, P.Align, Align.None);
        RunTime.Assign(two, P.Align, Align.None);
        var kept = (one.Get(P.Align), two.Get(P.Align));
        RunTime.Assign(one, P.Align, Align.Left);

        Assert.AreEqual((Align.Top, Align.Bottom), kept);
        Assert.AreEqual(("0,0 6000x3900", "0,3900 6000x600"), (At(layout, "PicM1"), At(layout, "PicM2")));
        Assert.AreEqual(387, Refused(() => RunTime.Assign(two, P.Width, 10d)));
    }

    [TestMethod]
    public void ABoxAProgramAdds_DocksInsideTheOthers_AndTheOthersTakeItsRoomAsItIsRemoved()
    {
        var layout = Laid(Probe);
        var added = layout.Form.AddControl(ComponentClasses.PictureBox, "PicNew");
        added.Set(P.Height, 300 / TwipsPerPixel).Set(P.Visible, true);
        RunTime.Assign(added, P.Align, Align.Top);
        var docked = (At(layout, "PicNew"), At(layout, "PicL"));
        layout.Form.RemoveControl(added);

        Assert.AreEqual(("0,1005 6000x300", "0,1305 900x2700"), docked);
        Assert.AreEqual("0,1005 900x3000", At(layout, "PicL"));
        Assert.IsFalse(layout.Docked.Contains(added));
    }

    [TestMethod]
    public void ALayoutThatDoesNotFollowTheForm_LaysOutWhenAsked()
    {
        var layout = Laid(Probe);
        layout.FollowsTheForm = false;
        Resize(layout, 4500, 4500);
        var before = At(layout, "PicT1");
        layout.LayOut();

        Assert.AreEqual(("0,405 6000x600", "0,405 4500x600"), (before, At(layout, "PicT1")));
    }

    [TestMethod]
    public void AProgramsAssignment_IsRefusedAsVB6RefusesIt_AndSetOtherwise()
    {
        var form = Read("""
            VERSION 5.00
            Begin VB.Form Form1
               Begin VB.Menu mnuFile
                  Caption         =   "&File"
                  Begin VB.Menu mnuSep
                     Caption         =   "-"
                  End
                  Begin VB.Menu mnuOpen
                     Caption         =   "Open"
                  End
               End
            End
            Attribute VB_Name = "Form1"
            """);
        ComponentInstance file = form.Components.First(c => c.Name == "mnuFile"), open = form.Components.First(c => c.Name == "mnuOpen"),
            separator = form.Components.First(c => c.Name == "mnuSep");

        Assert.AreEqual(382, Refused(() => RunTime.Assign(open, P.Shortcut, null)));
        Assert.AreEqual(380, Refused(() => RunTime.Assign(open, P.Caption, 5)));
        Assert.AreEqual(387, Refused(() => RunTime.Assign(file, P.Checked, true)));
        Assert.AreEqual(387, Refused(() => RunTime.Assign(separator, P.Enabled, false)));
        Assert.AreEqual(395, Refused(() => RunTime.Assign(open, P.Caption, "-")));
        Assert.ThrowsExactly<ArgumentException>(() => RunTime.Assign(open, P.Align, Align.Top));
        RunTime.Assign(open, P.Checked, true);
        RunTime.Assign(open, P.Tag, "t");
        Assert.AreEqual((true, "t"), (open.Get(P.Checked), open.Get(P.Tag)));
    }
}

using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// Control arrays of controls other than menus at run time, as measured with VB6 on this form: a TextBox that is no array's element, a
// PictureBox array, a CommandButton array in a Frame that the file lists 2 before 0, and a hidden Label array that starts at 1.
[TestClass]
public sealed class FormControlArrayTests
{
    private const string Probe = """
        VERSION 5.00
        Begin VB.Form Form1
           Caption         =   "Form1"
           Begin VB.TextBox txt
              TabIndex        =   0
              Text            =   "plain"
           End
           Begin VB.PictureBox picCell
              BackColor       =   &H000000FF&
              BorderStyle     =   0  'None
              Height          =   500
              Index           =   0
              Left            =   100
              TabIndex        =   1
              Tag             =   "cell"
              Top             =   100
              Width           =   500
           End
           Begin VB.Frame fra
              Caption         =   "fra"
              TabIndex        =   2
              Begin VB.CommandButton cmd
                 Caption         =   "C2"
                 Index           =   2
                 TabIndex        =   4
              End
              Begin VB.CommandButton cmd
                 Caption         =   "C0"
                 Index           =   0
                 TabIndex        =   3
              End
           End
           Begin VB.Label lbl
              Caption         =   "L1"
              Index           =   1
              TabIndex        =   5
              Visible         =   0   'False
           End
        End
        Attribute VB_Name = "Form1"
        """;

    private static FormDefinition Read() => VB6FormReader.Read(Probe.ReplaceLineEndings("\r\n")).Form!;

    private static int Error(Action act) => Assert.ThrowsExactly<FormErrorException>(act).Error.Number;

    private static string[] Indexes(FormControlArray array) => [.. array.Select(element => $"{element.Index}")];

    [TestMethod]
    public void AnArray_IsItsElementsInIndexOrder_WhateverTheFilesOrder()
    {
        var form = Read();

        var cmd = form.ArrayOf("CMD")!;

        Assert.AreEqual((2, 0, 2), (cmd.Count, cmd.LBound, cmd.UBound));
        Expect(["0", "2"], Indexes(cmd));
        Assert.AreEqual("C0", cmd[0].Get(P.Caption));
        Assert.AreEqual((1, 1, 1), (form.ArrayOf("lbl")!.Count, form.ArrayOf("lbl")!.LBound, form.ArrayOf("lbl")!.UBound));
        Assert.AreSame(cmd, form.ArrayOf("cmd"));
        Assert.AreEqual("cmd", cmd.Name);
    }

    [TestMethod]
    public void AControlOfNoArray_OrNoControl_HasNoArray()
    {
        var form = Read();

        Assert.IsNull(form.ArrayOf("txt"));
        Assert.IsNull(form.ArrayOf("fra"));
        Assert.IsNull(form.ArrayOf("nothing"));
    }

    [TestMethod]
    public void Load_CopiesTheLowestIndexAsItIsThen_Hidden_WithTheNextTabIndex_InItsContainer()
    {
        var form = Read();
        var events = new List<string>();
        form.ControlAdded += (_, e) => events.Add($"{e.Control} in {e.Container.Name}");
        var picCell = form.ArrayOf("picCell")!;
        picCell[0].Set(P.BackColor, FormColor.FromRgb(0, 255, 0)).Set(P.Left, 10d);

        var loaded = picCell.Load(1);

        Assert.AreSame(ComponentClasses.PictureBox, loaded.Class);
        Assert.AreEqual(("picCell", 1), (loaded.Name, loaded.Index));
        Assert.AreEqual(picCell[0].Get(P.BackColor), loaded.Get(P.BackColor));
        Assert.AreEqual((10d, picCell[0].Get(P.Top), picCell[0].Get(P.Width), picCell[0].Get(P.Height)),
            (loaded.Get(P.Left), loaded.Get(P.Top), loaded.Get(P.Width), loaded.Get(P.Height)));
        Assert.AreEqual(("cell", picCell[0].Get(P.BorderStyle)), (loaded.Get(P.Tag), loaded.Get(P.BorderStyle)));
        Assert.IsFalse(loaded.Get(P.Visible));
        Assert.AreEqual((short)6, loaded.Get(P.TabIndex));
        Assert.AreSame(form.Root, loaded.Container);
        Assert.IsTrue(form.IsAddedAtRunTime(loaded));
        Assert.AreSame(loaded, picCell[1]);
        Expect(["picCell(1) in Form1"], events);
    }

    // An element keeps its array's name, even one a program loaded: assigning its Name is 382, as a control of the form's file's is.
    [TestMethod]
    public void AnElementLoaded_KeepsItsArraysName_382()
    {
        var form = Read();
        var loaded = form.ArrayOf("picCell")!.Load(1);

        Assert.AreEqual(382, Error(() => form.RenameControl(loaded, "Other")));
        Assert.AreEqual("picCell", loaded.Name);
    }

    [TestMethod]
    public void Load_CopiesTheElementOfTheLowestIndexLoaded_NotTheFilesOne()
    {
        var form = Read();
        var cmd = form.ArrayOf("cmd")!;
        var lbl = form.ArrayOf("lbl")!;

        var inFrame = cmd.Load(1);
        cmd[0].Set(P.Caption, "C0 changed");
        var changed = cmd.Load(5);
        lbl.Load(0).Set(P.Caption, "L0 changed");
        var fromLoaded = lbl.Load(7);

        Assert.AreSame(form.Find("fra"), inFrame.Container);
        Assert.AreEqual("C0", inFrame.Get(P.Caption));
        Assert.AreEqual("C0 changed", changed.Get(P.Caption));
        Assert.AreEqual("L0 changed", fromLoaded.Get(P.Caption));
        Expect(["0", "1", "2", "5"], Indexes(cmd));
        Expect(["0", "1", "7"], Indexes(lbl));
        Expect(["txt", "picCell(0)", "fra", "cmd(2)", "cmd(0)", "lbl(1)", "cmd(1)", "cmd(5)", "lbl(0)", "lbl(7)"],
            form.Controls.Select(control => control.ToString()));
    }

    [TestMethod]
    public void Load_OfAHiddenElement_IsHidden_AndOfAVisibleOne_TooHidden()
    {
        var form = Read();
        var picCell = form.ArrayOf("picCell")!;

        picCell[0].Set(P.Visible, false);
        var fromHidden = picCell.Load(2);

        Assert.IsFalse(fromHidden.Get(P.Visible));
        Assert.IsFalse(form.ArrayOf("lbl")!.Load(3).Get(P.Visible));
    }

    [TestMethod]
    public void Load_IsRefusedAsVB6RefusesIt_AndChangesNothing()
    {
        var form = Read();
        var picCell = form.ArrayOf("picCell")!;
        picCell.Load(1);
        var raised = 0;
        form.ControlAdded += (_, _) => raised++;

        Assert.AreEqual(360, Error(() => picCell.Load(1)));
        Assert.AreEqual(360, Error(() => picCell.Load(0)));
        var negative = Assert.ThrowsExactly<FormErrorException>(() => picCell.Load(-1)).Error;
        Assert.AreEqual((341, "Invalid control array index"), (negative.Number, negative.Description));
        Assert.AreEqual(6, Error(() => picCell.Load(32768)));
        Assert.AreEqual(0, raised);
        Assert.AreEqual(2, picCell.Count);

        picCell.Load(32767);
        Assert.AreEqual((3, 32767), (picCell.Count, picCell.UBound));
    }

    [TestMethod]
    public void Unload_TakesALoadedElementOut_AndTheTabIndexesCloseUp_ButNeverOneOfTheFile()
    {
        var form = Read();
        var picCell = form.ArrayOf("picCell")!;
        var events = new List<string>();
        form.ControlRemoved += (_, e) => events.Add($"{e.Control} from {e.Container.Name}");
        picCell.Load(1);
        var later = form.ArrayOf("cmd")!.Load(1);

        picCell.Unload(1);

        Expect(["0"], Indexes(picCell));
        Assert.IsFalse(picCell.IsLoaded(1));
        Assert.AreEqual(340, Error(() => _ = picCell[1]));
        Assert.AreEqual((short)6, later.Get(P.TabIndex));
        Expect(["picCell(1) from Form1"], events);
        Assert.AreEqual(362, Error(() => picCell.Unload(0)));
        Assert.AreEqual(362, Error(() => form.ArrayOf("cmd")!.Unload(2)));
        Assert.AreEqual(340, Error(() => picCell.Unload(9)));

        var again = picCell.Load(1);
        Assert.AreEqual((short)7, again.Get(P.TabIndex));
    }

    [TestMethod]
    public void Load_OfAnArrayThatHasNoElementLeftToCopy_Is340_AndChangesNothing()
    {
        var form = Read();
        var picCell = form.ArrayOf("picCell")!;
        picCell[0].SetContainer(null);

        Assert.AreEqual(340, Error(() => picCell.Load(1)));
        Assert.AreEqual(0, picCell.Count);
        Assert.IsFalse(form.Controls.Any(control => control.Name == "picCell"));
    }

    [TestMethod]
    public void ControlsRemove_OfAnElement_Is729_EvenOneLoaded_AndOfTheArraysName_IsNoSuchInterface()
    {
        var form = Read();
        var picCell = form.ArrayOf("picCell")!;
        var loaded = picCell.Load(1);

        Assert.AreEqual(729, Error(() => form.RemoveControl(loaded)));
        Assert.AreEqual(729, Error(() => form.RemoveControl(picCell[0])));
        Assert.AreEqual(unchecked((int)0x80004002), Error(() => form.RemoveControl("picCell")));
        Assert.AreEqual((2, true), (picCell.Count, picCell.IsLoaded(1)));
        Assert.IsTrue(form.IsAddedAtRunTime(loaded));
    }

    private static void Expect(string[] expected, IEnumerable<string> actual) =>
        CollectionAssert.AreEqual(expected, actual.ToArray(), $"expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
}

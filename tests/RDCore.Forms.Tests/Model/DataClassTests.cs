using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// VB.Data's members as VB6.OLB's type information lists them, and its defaults and ranges as measured with VB6 by reading each property late
// bound after Load and assigning it, native and p-code agreeing.
[TestClass]
public sealed class DataClassTests
{
    private static readonly ComponentClass Data = VBClasses.Data;

    [TestMethod]
    public void TheDataClass_IsInTheCatalog_ByItsTypeName() =>
        Assert.AreSame(Data, VBClasses.ByTypeName("vb.data"));

    [TestMethod]
    public void ItsEvents_AreTheOnesOfItsTypeInformation() =>
        CollectionAssert.AreEquivalent(
            new[] { "Validate", "Error", "Reposition", "DragDrop", "DragOver", "MouseDown", "MouseMove", "MouseUp", "Resize", "OLEDragOver",
                "OLEDragDrop", "OLEGiveFeedback", "OLEStartDrag", "OLESetData", "OLECompleteDrag" },
            Data.Events.Select(e => e.Name).ToArray());

    [TestMethod]
    public void ItsValidateAndErrorEvents_TakeTheirTwoIntegers()
    {
        CollectionAssert.AreEqual(new[] { "Action", "Save" }, Data.Event("Validate")!.Parameters.Select(p => p.Name).ToArray());
        CollectionAssert.AreEqual(new[] { "DataErr", "Response" }, Data.Event("Error")!.Parameters.Select(p => p.Name).ToArray());
        Assert.IsTrue(Data.Event("Error")!.Parameters.All(p => p.TypeName == "Integer"));
    }

    [TestMethod]
    public void ItsMethods_AreAControlsAndItsTwoOwn_WithNoSetFocus() =>
        CollectionAssert.AreEquivalent(
            new[] { VBMethods.Move, VBMethods.Refresh, VBMethods.ZOrder, VBMethods.UpdateRecord, VBMethods.UpdateControls },
            Data.Methods.ToArray());

    [TestMethod]
    [DataRow("TabIndex")]
    [DataRow("TabStop")]
    [DataRow("BorderStyle")]
    [DataRow("BackStyle")]
    [DataRow("HelpContextID")]
    public void AMemberVB6RefusesWith438_IsNotAProperty(string name) =>
        Assert.IsNull(Data.Property(name));

    [TestMethod]
    public void ItsDefaults_AreTheMeasuredOnes()
    {
        Assert.AreEqual("Access", Data.DefaultOf(P.Connect));
        Assert.AreEqual("", Data.DefaultOf(P.DatabaseName));
        Assert.AreEqual("", Data.DefaultOf(P.RecordSource));
        Assert.AreEqual(DataRecordsetType.Dynaset, Data.DefaultOf(P.RecordsetType));
        Assert.AreEqual(DataDefaultType.UseJet, Data.DefaultOf(P.DefaultType));
        Assert.AreEqual(DataDefaultCursorType.Default, Data.DefaultOf(P.DefaultCursorType));
        Assert.AreEqual(DataBOFAction.MoveFirst, Data.DefaultOf(P.BOFAction));
        Assert.AreEqual(DataEOFAction.MoveLast, Data.DefaultOf(P.EOFAction));
        Assert.AreEqual((short)0, Data.DefaultOf(P.Options));
        Assert.IsFalse(Data.DefaultOf(P.Exclusive));
        Assert.AreEqual(Align.None, Data.DefaultOf(P.Align));
        Assert.AreEqual(Appearance.ThreeD, Data.DefaultOf(P.Appearance));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.Window), Data.DefaultOf(P.BackColor));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.WindowText), Data.DefaultOf(P.ForeColor));
    }

    [TestMethod]
    public void ItsReadOnly_DefaultsToFalse_WhereAFileListBoxsDefaultsToTrue()
    {
        Assert.IsFalse(Data.DefaultOf(P.ReadOnly));
        Assert.IsTrue(VBClasses.FileListBox.DefaultOf(P.ReadOnly));
    }

    [TestMethod]
    public void ItsDefaultMember_IsItsCaption() =>
        Assert.AreEqual("Caption", Data.DefaultMember);

    [TestMethod]
    [DataRow(0, true)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    [DataRow(-1, false)]
    public void ItsEOFAction_Takes0To2(int value, bool accepted) =>
        Assert.AreEqual(accepted, P.EOFAction.Accepts((DataEOFAction)value));

    [TestMethod]
    [DataRow(1, true)]
    [DataRow(2, false)]
    public void ItsBOFAction_Takes0And1(int value, bool accepted) =>
        Assert.AreEqual(accepted, P.BOFAction.Accepts((DataBOFAction)value));

    [TestMethod]
    [DataRow(2, true)]
    [DataRow(3, false)]
    public void ItsRecordsetType_Takes0To2(int value, bool accepted) =>
        Assert.AreEqual(accepted, P.RecordsetType.Accepts((DataRecordsetType)value));

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(1, true)]
    [DataRow(2, true)]
    [DataRow(3, false)]
    public void ItsDefaultType_Takes1And2(int value, bool accepted) =>
        Assert.AreEqual(accepted, P.DefaultType.Accepts((DataDefaultType)value));

    [TestMethod]
    public void ItsDefaultCursorType_TakesAValueThatNamesNone() =>
        Assert.IsTrue(P.DefaultCursorType.Accepts((DataDefaultCursorType)3));

    [TestMethod]
    public void ADesignerFilesDataControl_ReadsItsDatabaseProperties()
    {
        var import = VB6FormReader.Read(string.Join("\r\n", [
            "VERSION 5.00",
            "Begin VB.Form Form1 ",
            "   ClientHeight    =   3000",
            "   ClientWidth     =   4000",
            "   Begin VB.Data Data1 ",
            "      Caption         =   \"Customers\"",
            "      Connect         =   \"Access 2000;\"",
            "      DatabaseName    =   \"C:\\data\\shop.mdb\"",
            "      DefaultCursorType=   0  'DefaultCursor",
            "      DefaultType     =   2  'UseJet",
            "      EOFAction       =   2  'Add New",
            "      Exclusive       =   -1  'True",
            "      Height          =   345",
            "      Left            =   120",
            "      Options         =   64",
            "      ReadOnly        =   -1  'True",
            "      RecordsetType   =   2  'Snapshot",
            "      RecordSource    =   \"Customers\"",
            "      Top             =   120",
            "      Width           =   2400",
            "   End",
            "End",
            "Attribute VB_Name = \"Form1\"",
            ""]));
        var data = import.Form!.Find("Data1")!;

        Assert.IsEmpty(import.Diagnostics);
        Assert.AreSame(Data, data.Class);
        Assert.AreEqual("Customers", data.Get(P.Caption));
        Assert.AreEqual("Access 2000;", data.Get(P.Connect));
        Assert.AreEqual(@"C:\data\shop.mdb", data.Get(P.DatabaseName));
        Assert.AreEqual(DataEOFAction.AddNew, data.Get(P.EOFAction));
        Assert.IsTrue(data.Get(P.Exclusive));
        Assert.AreEqual((short)64, data.Get(P.Options));
        Assert.IsTrue(data.Get(P.ReadOnly));
        Assert.AreEqual(DataRecordsetType.Snapshot, data.Get(P.RecordsetType));
        Assert.AreEqual("Customers", data.Get(P.RecordSource));
    }
}

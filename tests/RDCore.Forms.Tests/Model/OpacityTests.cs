using RDCore.Forms.Import.VB6;
using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// Opacity, an RD-VBA addition: every control a running program shows has it, from 0 to 1, solid by default.
[TestClass]
public sealed class OpacityTests
{
    [TestMethod]
    public void EveryControlWithVisible_HasOpacity_AndNoRootMenuOrTimerHasIt()
    {
        var shown = VBClasses.All.Where(c => c.Kind == ComponentKind.Control && c.Has(P.Visible)).ToArray();
        var withOpacity = VBClasses.All.Where(c => c.Has(P.Opacity)).ToArray();

        CollectionAssert.AreEquivalent(shown, withOpacity);
        Assert.IsTrue(shown.Contains(VBClasses.Line));
        Assert.IsFalse(withOpacity.Any(c => c.Kind != ComponentKind.Control || ReferenceEquals(c, VBClasses.Timer)));
    }

    [TestMethod]
    public void APlacedUserControl_HasOpacity_UnlessItIsInvisibleAtRunTime()
    {
        static ComponentClass Placed(string invisible) => UserControlClasses.Of(VB6FormReader.Read(string.Join("\r\n", [
            "VERSION 5.00",
            "Begin VB.UserControl UserControl1 ",
            $"   InvisibleAtRuntime=   {invisible}",
            "   ScaleHeight     =   3600",
            "   ScaleWidth      =   4800",
            "End",
            "Attribute VB_Name = \"UserControl1\"",
            string.Empty])).Form!, "Project1");

        Assert.IsTrue(Placed("0   'False").Has(P.Opacity));
        Assert.IsFalse(Placed("-1  'True").Has(P.Opacity));
    }

    [TestMethod]
    public void Opacity_IsOne_ByDefault()
        => Assert.AreEqual(1d, new ComponentInstance(VBClasses.Label, "Label1").Get(P.Opacity));

    [TestMethod]
    [DataRow(0d)]
    [DataRow(0.25)]
    [DataRow(1d)]
    public void AnOpacityFrom0To1_IsTaken(double opacity)
    {
        var label = new ComponentInstance(VBClasses.Label, "Label1");
        VBRunTime.Assign(label, P.Opacity, opacity);

        Assert.AreEqual(opacity, label.Get(P.Opacity));
    }

    [TestMethod]
    [DataRow(-0.01)]
    [DataRow(1.01)]
    [DataRow(double.NaN)]
    public void AnOpacityOutside0To1_Is380_AndTheDesignerRefusesIt(double opacity)
    {
        var label = new ComponentInstance(VBClasses.Label, "Label1").Set(P.Opacity, 0.5);

        Assert.AreEqual(380, Assert.ThrowsExactly<FormErrorException>(() => VBRunTime.Assign(label, P.Opacity, opacity)).Error.Number);
        Assert.IsFalse(label.TrySet(P.Opacity, opacity));
        Assert.AreEqual(0.5, label.Get(P.Opacity));
    }

    [TestMethod]
    [DataRow("0.25", 0.25, false)]
    [DataRow("2", 1d, true)]
    public void AFormsFile_GivesOpacity_AndKeepsOneItCannotHoldAsWritten(string written, double read, bool unmodelled)
    {
        var form = VB6FormReader.Read(string.Join("\r\n", [
            "VERSION 5.00",
            "Begin VB.Form Form1 ",
            "   ClientHeight    =   3000",
            "   ClientWidth     =   4000",
            "   Begin VB.Label Label1 ",
            "      Caption         =   \"Label1\"",
            "      Height          =   300",
            "      Left            =   120",
            $"      Opacity         =   {written}",
            "      Top             =   120",
            "      Width           =   900",
            "   End",
            "End",
            "Attribute VB_Name = \"Form1\"",
            string.Empty])).Form!;
        var label = form.Find("Label1")!;

        Assert.AreEqual(read, label.Get(P.Opacity));
        Assert.AreEqual(unmodelled, label.UnmodelledProperties.Any(p => p.Name == "Opacity"));
    }
}

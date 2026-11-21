using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// A running program's BackColor on a transparent Label is dropped, with no error: it reads back the colour the label had, and becoming opaque later
// shows that colour. Measured with VB6, its original release and SP6, native and p-code agreeing: a label transparent from its form or made transparent
// by the program, and an element of a control array alike. A form's file sets it all the same.
[TestClass]
public sealed class RunTimeLabelBackColorTests
{
    private static readonly FormColor Red = FormColor.FromRgb(255, 0, 0);
    private static readonly FormColor Green = FormColor.FromRgb(0, 255, 0);
    private static readonly FormColor Blue = FormColor.FromRgb(0, 0, 255);

    private static ComponentInstance Label(BackStyle backStyle, int? index = null)
        => new ComponentInstance(ComponentClasses.Label, "Label1", index).Set(P.BackStyle, backStyle).Set(P.BackColor, Red);

    [TestMethod]
    public void ABackColor_AssignedToATransparentLabel_IsDropped_AndBecomingOpaqueShowsTheColourItHad()
    {
        var label = Label(BackStyle.Transparent);

        RunTime.Assign(label, P.BackColor, Blue);
        Assert.AreEqual(Red, label.Get(P.BackColor));

        RunTime.Assign(label, P.BackStyle, BackStyle.Opaque);
        Assert.AreEqual(Red, label.Get(P.BackColor));
    }

    [TestMethod]
    public void ALabelMadeTransparentByTheProgram_DropsABackColor_AndTakesOneOnceOpaqueAgain()
    {
        var label = Label(BackStyle.Opaque);

        RunTime.Assign(label, P.BackStyle, BackStyle.Transparent);
        RunTime.Assign(label, P.BackColor, Blue);
        Assert.AreEqual(Red, label.Get(P.BackColor));

        RunTime.Assign(label, P.BackStyle, BackStyle.Opaque);
        RunTime.Assign(label, P.BackColor, Green);
        Assert.AreEqual(Green, label.Get(P.BackColor));
    }

    [TestMethod]
    public void ALabelMadeOpaqueFirst_TakesTheBackColor()
    {
        var label = Label(BackStyle.Transparent);

        RunTime.Assign(label, P.BackStyle, BackStyle.Opaque);
        RunTime.Assign(label, P.BackColor, Blue);

        Assert.AreEqual(Blue, label.Get(P.BackColor));
    }

    [TestMethod]
    public void AnElementOfAControlArray_DropsABackColor_AssignedWhileTransparent()
    {
        var cell = Label(BackStyle.Transparent, index: 0);

        RunTime.Assign(cell, P.BackColor, Blue);
        RunTime.Assign(cell, P.BackStyle, BackStyle.Opaque);

        Assert.AreEqual(Red, cell.Get(P.BackColor));
    }

    [TestMethod]
    public void ATransparentLabelWithTheDefaultColour_KeepsTheDefault()
    {
        var label = new ComponentInstance(ComponentClasses.Label, "Label1").Set(P.BackStyle, BackStyle.Transparent);

        RunTime.Assign(label, P.BackColor, Blue);

        Assert.AreEqual(ComponentClasses.Label.DefaultOf(P.BackColor), label.Get(P.BackColor));
        Assert.IsFalse(label.IsSet(P.BackColor));
        Assert.AreEqual(BackStyle.Transparent, label.Get(P.BackStyle));
    }

    [TestMethod]
    public void TheRule_IsARunningProgramsOnly_AndATransparentLabelsOwn()
    {
        var label = Label(BackStyle.Transparent);

        Assert.IsTrue(RunTime.Drops(label, P.BackColor));
        Assert.IsFalse(RunTime.Drops(label, P.ForeColor));
        Assert.IsFalse(RunTime.Drops(Label(BackStyle.Opaque), P.BackColor));
        Assert.IsFalse(RunTime.Drops(new ComponentInstance(ComponentClasses.Shape, "Shape1").Set(P.BackStyle, BackStyle.Transparent), P.BackColor));

        // a form's file, which the designer and the import set through the model, gives a transparent label its colour.
        Assert.IsTrue(label.TrySet(P.BackColor, Blue));
        Assert.AreEqual(Blue, label.Get(P.BackColor));
    }

    [TestMethod]
    public void ADroppedBackColor_IsStillCheckedFirst()
        => Assert.AreEqual(FormError.InvalidPropertyValue,
            Assert.ThrowsExactly<FormErrorException>(() => RunTime.Assign(Label(BackStyle.Transparent), P.BackColor, "red")).Error);
}

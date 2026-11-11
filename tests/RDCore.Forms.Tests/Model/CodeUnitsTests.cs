using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// A position or a size, from the model's pixels to a program's unit and back.
[TestClass]
public sealed class CodeUnitsTests
{
    private static ComponentInstance Form(ScaleMode mode = ScaleMode.Twip) =>
        new ComponentInstance(VBClasses.Form, "Form1").Set(P.Width, 320d).Set(P.Height, 240d).Set(P.ScaleMode, mode);

    private static ComponentInstance In(ComponentInstance container, ComponentClass type, string name)
    {
        var control = new ComponentInstance(type, name).Set(P.Left, 160d).Set(P.Top, 120d).Set(P.Width, 32d).Set(P.Height, 16d);
        control.SetContainer(container);
        return control;
    }

    private static readonly PropertyDefinition<double>[] Extents = [P.Left, P.Top, P.Width, P.Height];

    private static double[] ToCode(ComponentInstance component, CodeUnit unit) =>
        [.. Extents.Select(property => CodeUnits.ToCode(component, property, component.Get(property), unit))];

    private static void AssertRoundTrip(ComponentInstance component, CodeUnit unit)
    {
        foreach (var property in Extents)
        {
            var code = CodeUnits.ToCode(component, property, component.Get(property), unit);
            Assert.AreEqual(component.Get(property), CodeUnits.FromCode(component, property, code, unit), 1e-9, property.Name);
        }
    }

    [TestMethod]
    [DataRow(CodeUnit.Pixel, new[] { 160d, 120d, 32d, 16d })]
    [DataRow(CodeUnit.Point, new[] { 120d, 90d, 24d, 12d })]
    [DataRow(CodeUnit.VB6, new[] { 2400d, 1800d, 480d, 240d })]
    public void AControlInATwipForm_IsInPixels_Points_OrTwips(CodeUnit unit, double[] expected)
    {
        var control = In(Form(), VBClasses.CommandButton, "Command1");

        CollectionAssert.AreEqual(expected, ToCode(control, unit));
        AssertRoundTrip(control, unit);
    }

    [TestMethod]
    public void InVB6_AFormsOwnPositionAndSize_AreTwips_WhateverItsScale()
    {
        var root = Form(ScaleMode.Pixel).Set(P.Left, 10d).Set(P.Top, 20d);

        CollectionAssert.AreEqual(new[] { 150d, 300d, 4800d, 3600d }, ToCode(root, CodeUnit.VB6));
        AssertRoundTrip(root, CodeUnit.VB6);
    }

    [TestMethod]
    public void InVB6_AControlInAFrame_IsInTwips_WhateverTheFormsScale()
    {
        var frame = In(Form(ScaleMode.Pixel), VBClasses.Frame, "Frame1");
        var control = In(frame, VBClasses.CommandButton, "Command1");

        CollectionAssert.AreEqual(new[] { 2400d, 1800d, 480d, 240d }, ToCode(control, CodeUnit.VB6));
    }

    [TestMethod]
    [DataRow(ScaleMode.Pixel, new[] { 160d, 120d, 32d, 16d })]
    [DataRow(ScaleMode.Point, new[] { 120d, 90d, 24d, 12d })]
    [DataRow(ScaleMode.Character, new[] { 20d, 7.5d, 4d, 1d })]
    public void InVB6_AControlInAForm_IsInTheFormsScale(ScaleMode mode, double[] expected)
    {
        var control = In(Form(mode), VBClasses.CommandButton, "Command1");

        CollectionAssert.AreEqual(expected, ToCode(control, CodeUnit.VB6));
        AssertRoundTrip(control, CodeUnit.VB6);
    }

    [TestMethod]
    public void InVB6_AUserScale_PlacesAControlFromItsScaleLeftAndTop_AndSizesItInItsUnits()
    {
        // 100 units across the form's 4800 twips (48 twips a unit) from 10, and 50 down its 3600 (72 twips a unit) from 20.
        var form = Form(ScaleMode.User).Set(P.ScaleLeft, 10d).Set(P.ScaleTop, 20d).Set(P.ScaleWidth, 100d).Set(P.ScaleHeight, 50d);
        var control = In(form, VBClasses.CommandButton, "Command1");

        CollectionAssert.AreEqual(new[] { 60d, 45d, 10d, 10d / 3 }, ToCode(control, CodeUnit.VB6));
        AssertRoundTrip(control, CodeUnit.VB6);
    }

    [TestMethod]
    public void InVB6_AScaleThatRunsRightToLeft_GivesASizeNoSign()
    {
        var form = Form(ScaleMode.User).Set(P.ScaleLeft, 100d).Set(P.ScaleTop, 0d).Set(P.ScaleWidth, -100d).Set(P.ScaleHeight, 50d);
        var control = In(form, VBClasses.CommandButton, "Command1");

        Assert.AreEqual(50d, CodeUnits.ToCode(control, P.Left, 160d, CodeUnit.VB6));
        Assert.AreEqual(10d, CodeUnits.ToCode(control, P.Width, 32d, CodeUnit.VB6));
    }

    [TestMethod]
    public void AnyOtherProperty_IsTheSameForCode()
    {
        var form = Form();

        Assert.IsFalse(CodeUnits.IsExtent(P.ScaleWidth));
        Assert.AreEqual(4800d, CodeUnits.ToCode(form, P.ScaleWidth, 4800d, CodeUnit.Point));
        Assert.AreEqual(4800d, CodeUnits.FromCode(form, P.ScaleWidth, 4800d, CodeUnit.Point));
    }
}

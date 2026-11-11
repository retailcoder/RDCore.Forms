using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class MethodDefinitionTests
{
    [TestMethod]
    public void ARequiredParameter_AfterAnOptionalOne_IsRefused()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new MethodDefinition("Go", new MethodParameter("A", "Variant", IsOptional: true), new MethodParameter("B", "Single")));
    }

    [TestMethod]
    public void RequiredCount_IsTheParametersBeforeTheFirstOptionalOne()
    {
        Assert.AreEqual(1, VBMethods.Move.RequiredCount);
        Assert.AreEqual(0, VBMethods.ZOrder.RequiredCount);
    }

    [TestMethod]
    public void TwoMethodsOfOneName_AreRefused_WhateverTheirCase()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ComponentClass("Test.Widget", "Widget", ComponentKind.Control, [], [], methods: [new MethodDefinition("Go"), new MethodDefinition("GO")]));
    }

    [TestMethod]
    public void Method_IsFoundWithoutRegardToCase()
    {
        Assert.AreSame(VBMethods.SetFocus, VBClasses.TextBox.Method("setfocus"));
        Assert.IsNull(VBClasses.Timer.Method("Move"));
    }

    [TestMethod]
    public void AControlThatCanTakeTheFocus_HasSetFocus_AndOneThatCannot_HasNot()
    {
        Assert.IsNotNull(VBClasses.CommandButton.Method("SetFocus"));
        Assert.IsNotNull(VBClasses.PictureBox.Method("SetFocus"));
        Assert.IsNull(VBClasses.Label.Method("SetFocus"));
        Assert.IsNull(VBClasses.Frame.Method("SetFocus"));
        Assert.IsNull(VBClasses.Shape.Method("SetFocus"));
    }

    [TestMethod]
    public void EveryControl_HasMoveRefreshAndZOrder_ALineNoMove_AndAFormNoZOrder()
    {
        var methods = (ComponentClass type) => string.Join(", ", type.Methods.Select(m => m.Name));

        Assert.AreEqual("Move, Refresh, ZOrder", methods(VBClasses.Image));
        Assert.AreEqual("Move, Refresh, SetFocus, ZOrder", methods(VBClasses.TextBox));
        Assert.AreEqual("Refresh, ZOrder", methods(VBClasses.Line));
        Assert.AreEqual("Move, Refresh, SetFocus", methods(VBClasses.Form));
        Assert.AreEqual("", methods(VBClasses.Menu));
    }
}

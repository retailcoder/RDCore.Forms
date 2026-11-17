using RDCore.Forms.Model;

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
        Assert.AreEqual(1, Methods.Move.RequiredCount);
        Assert.AreEqual(0, Methods.ZOrder.RequiredCount);
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
        Assert.AreSame(Methods.SetFocus, ComponentClasses.TextBox.Method("setfocus"));
        Assert.IsNull(ComponentClasses.Timer.Method("Move"));
    }

    [TestMethod]
    public void AControlThatCanTakeTheFocus_HasSetFocus_AndOneThatCannot_HasNot()
    {
        Assert.IsNotNull(ComponentClasses.CommandButton.Method("SetFocus"));
        Assert.IsNotNull(ComponentClasses.PictureBox.Method("SetFocus"));
        Assert.IsNull(ComponentClasses.Label.Method("SetFocus"));
        Assert.IsNull(ComponentClasses.Frame.Method("SetFocus"));
        Assert.IsNull(ComponentClasses.Shape.Method("SetFocus"));
    }

    [TestMethod]
    public void EveryControl_HasMoveRefreshAndZOrder_ALineNoMove_AFormNoZOrder_AndAnMDIFormNoRefresh()
    {
        var methods = (ComponentClass type) => string.Join(", ", type.Methods.Select(m => m.Name));

        Assert.AreEqual("Move, Refresh, ZOrder", methods(ComponentClasses.Image));
        Assert.AreEqual("Move, Refresh, SetFocus, ZOrder", methods(ComponentClasses.TextBox));
        Assert.AreEqual("Refresh, ZOrder", methods(ComponentClasses.Line));
        Assert.AreEqual("Move, Refresh, SetFocus", methods(ComponentClasses.Form));
        Assert.AreEqual("Move, SetFocus", methods(ComponentClasses.MDIForm));
        Assert.AreEqual("", methods(ComponentClasses.Menu));
    }
}

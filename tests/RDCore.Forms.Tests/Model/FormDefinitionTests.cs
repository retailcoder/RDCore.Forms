using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class FormDefinitionTests
{
    [TestMethod]
    public void AForm_IsDefinedByARoot_NotByAControl()
    {
        var control = new ComponentInstance(VBClasses.CommandButton, "Command1");

        Assert.ThrowsExactly<ArgumentException>(() => new FormDefinition(control));
    }

    [TestMethod]
    public void Components_AreTheRootThenItsContents_DepthFirst()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1");
        var frame = new ComponentInstance(VBClasses.Frame, "Frame1");
        var option = new ComponentInstance(VBClasses.OptionButton, "Option1", index: 0);
        var command = new ComponentInstance(VBClasses.CommandButton, "Command1");
        frame.SetContainer(root);
        option.SetContainer(frame);
        command.SetContainer(root);

        var form = new FormDefinition(root);

        CollectionAssert.AreEqual(new[] { root, frame, option, command }, form.Components.ToArray());
        Assert.AreEqual("Form1", form.Name);
    }

    [TestMethod]
    public void Find_TellsTheElementsOfAControlArrayApart()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1");
        var first = new ComponentInstance(VBClasses.OptionButton, "Option1", index: 0);
        var second = new ComponentInstance(VBClasses.OptionButton, "Option1", index: 1);
        first.SetContainer(root);
        second.SetContainer(root);
        var form = new FormDefinition(root);

        Assert.AreSame(second, form.Find("OPTION1", 1));
        Assert.IsNull(form.Find("Option1"));
    }

    [TestMethod]
    [DataRow(ScaleMode.Twip, 1d, 1d)]
    [DataRow(ScaleMode.Pixel, 15d, 15d)]
    [DataRow(ScaleMode.Character, 120d, 240d)]
    [DataRow(ScaleMode.Inch, 1440d, 1440d)]
    public void AScaleModesUnit_IsAConstantNumberOfTwips(ScaleMode mode, double across, double down)
    {
        Assert.AreEqual((across, down), mode.TwipsPerUnit());
    }

    [TestMethod]
    public void AUserScale_HasNoConstantUnit()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ScaleMode.User.TwipsPerUnit());
    }
}

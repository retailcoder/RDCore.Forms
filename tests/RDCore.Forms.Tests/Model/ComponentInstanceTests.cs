using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class ComponentInstanceTests
{
    [TestMethod]
    public void AnUnsetProperty_HasItsClassesDefault_WhereTheClassGivesOne()
    {
        var text = new ComponentInstance(VBClasses.TextBox, "Text1");
        var command = new ComponentInstance(VBClasses.CommandButton, "Command1");

        Assert.AreEqual(FormColor.FromSystem(SystemColor.Window), text.Get(VBProperties.BackColor));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.ButtonFace), command.Get(VBProperties.BackColor));
        Assert.IsFalse(text.IsSet(VBProperties.BackColor));
    }

    [TestMethod]
    public void SetThenReset_RaisesTheChangeBothWays_AndRestoresTheDefault()
    {
        var label = new ComponentInstance(VBClasses.Label, "Label1");
        var changes = new List<(object? Before, object? After)>();
        label.PropertyChanged += (_, e) => changes.Add((e.Before, e.After));

        label.Set(VBProperties.Caption, "Name:");
        var reset = label.Reset(VBProperties.Caption);

        Assert.IsTrue(reset);
        Assert.AreEqual("", label.Get(VBProperties.Caption));
        CollectionAssert.AreEqual(new List<(object?, object?)> { ("", "Name:"), ("Name:", "") }, changes);
    }

    [TestMethod]
    public void TrySet_RefusesAValueOfAnotherType_AndAPropertyTheClassHasNot()
    {
        var check = new ComponentInstance(VBClasses.CheckBox, "Check1");

        var wrongType = check.TrySet(VBProperties.CheckBoxValue, true);
        var foreign = check.TrySet(VBProperties.OptionButtonValue, true);
        var accepted = check.TrySet(VBProperties.CheckBoxValue, CheckBoxValue.Grayed);

        Assert.IsFalse(wrongType);
        Assert.IsFalse(foreign);
        Assert.IsTrue(accepted);
        Assert.AreEqual(CheckBoxValue.Grayed, check.Get(VBProperties.CheckBoxValue));
    }

    [TestMethod]
    public void Get_OfAPropertyTheClassHasNot_Throws()
    {
        var line = new ComponentInstance(VBClasses.Line, "Line1");

        Assert.ThrowsExactly<ArgumentException>(() => line.Get(VBProperties.Left));
    }

    [TestMethod]
    public void SetContainer_KeepsBothDirections_WhenAControlMovesBetweenContainers()
    {
        var form = new ComponentInstance(VBClasses.Form, "Form1");
        var frame = new ComponentInstance(VBClasses.Frame, "Frame1");
        var option = new ComponentInstance(VBClasses.OptionButton, "Option1");
        frame.SetContainer(form);
        option.SetContainer(form);

        option.SetContainer(frame);

        Assert.AreSame(frame, option.Container);
        CollectionAssert.AreEqual(new[] { frame }, form.Contents.ToArray());
        CollectionAssert.AreEqual(new[] { option }, frame.Contents.ToArray());
    }

    [TestMethod]
    public void SetContainer_AtAnIndex_InsertsThere()
    {
        var form = new ComponentInstance(VBClasses.Form, "Form1");
        var first = new ComponentInstance(VBClasses.Label, "Label1");
        var second = new ComponentInstance(VBClasses.Label, "Label2");
        first.SetContainer(form);

        second.SetContainer(form, 0);

        CollectionAssert.AreEqual(new[] { second, first }, form.Contents.ToArray());
    }

    [TestMethod]
    public void SetContainer_RefusesAPlacementAFormCannotHold()
    {
        var form = new ComponentInstance(VBClasses.Form, "Form1");
        var other = new ComponentInstance(VBClasses.Form, "Form2");
        var label = new ComponentInstance(VBClasses.Label, "Label1");
        var text = new ComponentInstance(VBClasses.TextBox, "Text1");
        var menu = new ComponentInstance(VBClasses.Menu, "mnuFile");
        var frame = new ComponentInstance(VBClasses.Frame, "Frame1");
        var inner = new ComponentInstance(VBClasses.Frame, "Frame2");
        frame.SetContainer(form);
        inner.SetContainer(frame);

        Assert.ThrowsExactly<InvalidOperationException>(() => text.SetContainer(label));
        Assert.ThrowsExactly<InvalidOperationException>(() => menu.SetContainer(frame));
        Assert.ThrowsExactly<InvalidOperationException>(() => other.SetContainer(form));
        Assert.ThrowsExactly<InvalidOperationException>(() => frame.SetContainer(inner));
    }

    [TestMethod]
    public void AMenuItem_IsPlacedInARootOrInAnotherMenuItem()
    {
        var form = new ComponentInstance(VBClasses.Form, "Form1");
        var file = new ComponentInstance(VBClasses.Menu, "mnuFile");
        var open = new ComponentInstance(VBClasses.Menu, "mnuOpen");

        file.SetContainer(form);
        open.SetContainer(file);

        CollectionAssert.AreEqual(new[] { file, open }, form.Descendants().ToArray());
    }

    [TestMethod]
    public void Descendants_AreDepthFirstInFileOrder()
    {
        var form = new ComponentInstance(VBClasses.Form, "Form1");
        var frame = new ComponentInstance(VBClasses.Frame, "Frame1");
        var inner = new ComponentInstance(VBClasses.CommandButton, "Command1");
        var after = new ComponentInstance(VBClasses.Label, "Label1");
        frame.SetContainer(form);
        inner.SetContainer(frame);
        after.SetContainer(form);

        CollectionAssert.AreEqual(new[] { frame, inner, after }, form.Descendants().ToArray());
    }

    [TestMethod]
    public void Descendants_OfTenThousandNestedFrames_AreReadOnTheDefaultStack()
    {
        const int depth = 10_000;
        var form = new ComponentInstance(VBClasses.Form, "Form1");
        var container = form;
        for (var level = 0; level < depth; level++)
        {
            var frame = new ComponentInstance(VBClasses.Frame, "Frame1", level);
            frame.SetContainer(container);
            container = frame;
        }

        var count = 0;
        var thread = new Thread(() => count = form.Descendants().Count());
        thread.Start();
        thread.Join();

        Assert.AreEqual(depth, count);
    }

    [TestMethod]
    public void ToString_NamesTheControlArrayElement()
    {
        var option = new ComponentInstance(VBClasses.OptionButton, "Option1", index: 2);

        Assert.AreEqual("Option1(2)", option.ToString());
    }
}

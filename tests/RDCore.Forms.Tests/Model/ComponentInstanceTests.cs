using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class ComponentInstanceTests
{
    [TestMethod]
    public void AnUnsetProperty_HasItsClassesDefault_WhereTheClassGivesOne()
    {
        var text = new ComponentInstance(ComponentClasses.TextBox, "Text1");
        var command = new ComponentInstance(ComponentClasses.CommandButton, "Command1");

        Assert.AreEqual(FormColor.FromSystem(SystemColor.Window), text.Get(Properties.BackColor));
        Assert.AreEqual(FormColor.FromSystem(SystemColor.ButtonFace), command.Get(Properties.BackColor));
        Assert.IsFalse(text.IsSet(Properties.BackColor));
    }

    [TestMethod]
    public void SetThenReset_RaisesTheChangeBothWays_AndRestoresTheDefault()
    {
        var label = new ComponentInstance(ComponentClasses.Label, "Label1");
        var changes = new List<(object? Before, object? After)>();
        label.PropertyChanged += (_, e) => changes.Add((e.Before, e.After));

        label.Set(Properties.Caption, "Name:");
        var reset = label.Reset(Properties.Caption);

        Assert.IsTrue(reset);
        Assert.AreEqual("", label.Get(Properties.Caption));
        CollectionAssert.AreEqual(new List<(object?, object?)> { ("", "Name:"), ("Name:", "") }, changes);
    }

    [TestMethod]
    public void TrySet_RefusesAValueOfAnotherType_AndAPropertyTheClassHasNot()
    {
        var check = new ComponentInstance(ComponentClasses.CheckBox, "Check1");

        var wrongType = check.TrySet(Properties.CheckBoxValue, true);
        var foreign = check.TrySet(Properties.OptionButtonValue, true);
        var accepted = check.TrySet(Properties.CheckBoxValue, CheckBoxValue.Grayed);

        Assert.IsFalse(wrongType);
        Assert.IsFalse(foreign);
        Assert.IsTrue(accepted);
        Assert.AreEqual(CheckBoxValue.Grayed, check.Get(Properties.CheckBoxValue));
    }

    [TestMethod]
    public void Get_OfAPropertyTheClassHasNot_Throws()
    {
        var line = new ComponentInstance(ComponentClasses.Line, "Line1");

        Assert.ThrowsExactly<ArgumentException>(() => line.Get(Properties.Left));
    }

    [TestMethod]
    public void SetContainer_KeepsBothDirections_WhenAControlMovesBetweenContainers()
    {
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");
        var frame = new ComponentInstance(ComponentClasses.Frame, "Frame1");
        var option = new ComponentInstance(ComponentClasses.OptionButton, "Option1");
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
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");
        var first = new ComponentInstance(ComponentClasses.Label, "Label1");
        var second = new ComponentInstance(ComponentClasses.Label, "Label2");
        first.SetContainer(form);

        second.SetContainer(form, 0);

        CollectionAssert.AreEqual(new[] { second, first }, form.Contents.ToArray());
    }

    [TestMethod]
    public void SetContainer_RefusesAPlacementAFormCannotHold()
    {
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");
        var other = new ComponentInstance(ComponentClasses.Form, "Form2");
        var label = new ComponentInstance(ComponentClasses.Label, "Label1");
        var text = new ComponentInstance(ComponentClasses.TextBox, "Text1");
        var menu = new ComponentInstance(ComponentClasses.Menu, "mnuFile");
        var frame = new ComponentInstance(ComponentClasses.Frame, "Frame1");
        var inner = new ComponentInstance(ComponentClasses.Frame, "Frame2");
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
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");
        var file = new ComponentInstance(ComponentClasses.Menu, "mnuFile");
        var open = new ComponentInstance(ComponentClasses.Menu, "mnuOpen");

        file.SetContainer(form);
        open.SetContainer(file);

        CollectionAssert.AreEqual(new[] { file, open }, form.Descendants().ToArray());
    }

    [TestMethod]
    public void Descendants_AreDepthFirstInFileOrder()
    {
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");
        var frame = new ComponentInstance(ComponentClasses.Frame, "Frame1");
        var inner = new ComponentInstance(ComponentClasses.CommandButton, "Command1");
        var after = new ComponentInstance(ComponentClasses.Label, "Label1");
        frame.SetContainer(form);
        inner.SetContainer(frame);
        after.SetContainer(form);

        CollectionAssert.AreEqual(new[] { frame, inner, after }, form.Descendants().ToArray());
    }

    [TestMethod]
    public void Descendants_OfTenThousandNestedFrames_AreReadOnTheDefaultStack()
    {
        const int depth = 10_000;
        var form = new ComponentInstance(ComponentClasses.Form, "Form1");
        var container = form;
        for (var level = 0; level < depth; level++)
        {
            var frame = new ComponentInstance(ComponentClasses.Frame, "Frame1", level);
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
        var option = new ComponentInstance(ComponentClasses.OptionButton, "Option1", index: 2);

        Assert.AreEqual("Option1(2)", option.ToString());
    }
}

using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// Controls.Add and Controls.Remove as measured with VB6, native and p-code agreeing, on a form that holds Frame1 (TabIndex 1) then
// Command1 (TabIndex 0) at design time.
[TestClass]
public sealed class RunTimeControlTests
{
    private static FormDefinition Form()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        new ComponentInstance(ComponentClasses.Frame, "Frame1").Set(P.TabIndex, (short)1).SetContainer(root);
        new ComponentInstance(ComponentClasses.CommandButton, "Command1").Set(P.TabIndex, (short)0).SetContainer(root);
        return new FormDefinition(root);
    }

    private static string[] Names(FormDefinition form) => [.. form.Controls.Select(c => c.Name)];

    private static int Error(Action act) => Assert.ThrowsExactly<FormErrorException>(act).Error.Number;

    [TestMethod]
    public void AControlAdded_IsHidden_At00_80By32Pixels_WithTheNextTabIndex_AndAnEmptyCaption()
    {
        var form = Form();
        var events = new List<string>();
        form.ControlAdded += (_, e) => events.Add($"{e.Control.Name} in {e.Container.Name}");

        var added = form.AddControl("rdforms.commandbutton", "cmdNew");

        Assert.AreSame(ComponentClasses.CommandButton, added.Class);
        Assert.IsFalse(added.Get(P.Visible));
        Assert.AreEqual((0d, 0d, 80d, 32d), (added.Get(P.Left), added.Get(P.Top), added.Get(P.Width), added.Get(P.Height)));
        Assert.AreEqual((short)2, added.Get(P.TabIndex));
        Assert.AreEqual("", added.Get(P.Caption));
        Assert.AreSame(form.Root, added.Container);
        Assert.IsTrue(form.IsAddedAtRunTime(added));
        Assert.IsFalse(form.IsAddedAtRunTime(form.Find("Command1")!));
        Expect(["cmdNew in Form1"], events);
    }

    [TestMethod]
    public void TheControls_AreTheFormsFileDepthFirst_ThenThoseAdded_InTheOrderAdded_WhateverTheirContainer()
    {
        var form = Form();
        form.Find("Frame1")!.SetContainer(form.Root, 0);
        new ComponentInstance(ComponentClasses.OptionButton, "Option1").SetContainer(form.Find("Frame1")!);

        form.AddControl("RDForms.CommandButton", "cmdNew");
        var inFrame = form.AddControl("RDForms.CommandButton", "cmdIn", form.Find("Frame1"));
        form.AddControl("RDForms.Label", "lblNew");

        Expect(["Frame1", "Option1", "Command1", "cmdNew", "cmdIn", "lblNew"], Names(form));
        Assert.AreSame(form.Find("Frame1"), inFrame.Container);
        Assert.AreEqual((short)4, inFrame.Get(P.TabIndex));
    }

    [TestMethod]
    public void AControlAdded_TakesAnyNameButANameTaken_OrEmpty_EvenTheFormsOwn()
    {
        var form = Form();

        foreach (var name in new[] { "1abc", "a b", "_a", "a$", new string('m', 41), "Sub", "Form1", "Me" })
            form.AddControl("RDForms.CommandButton", name);

        Assert.AreEqual(727, Error(() => form.AddControl("RDForms.CommandButton", "frame1")));
        Assert.AreEqual(727, Error(() => form.AddControl("RDForms.CommandButton", "A B")));
        Assert.AreEqual(728, Error(() => form.AddControl("RDForms.CommandButton", "")));
        Assert.AreEqual(728, Error(() => form.AddControl("RDForms.CommandButton", "  ")));
        Assert.AreEqual(10, form.Controls.Count);
    }

    [TestMethod]
    public void AClassOrAContainerControlsAddCannotTake_IsRefused_AndAddsNothing()
    {
        var form = Form();
        var button = form.AddControl("RDForms.CommandButton", "cmdNew");
        var raised = 0;
        form.ControlAdded += (_, _) => raised++;

        Assert.AreEqual(711, Error(() => form.AddControl("RDForms.Bogus", "x1")));
        // a VB6 ProgID is the port's to rewrite
        Assert.AreEqual(711, Error(() => form.AddControl("VB.CommandButton", "x2")));
        Assert.AreEqual(720, Error(() => form.AddControl("RDForms.Menu", "mnuNew")));
        Assert.AreEqual(720, Error(() => form.AddControl("RDForms.Form", "frmNew")));
        Assert.AreEqual(721, Error(() => form.AddControl("RDForms.CommandButton", "cmdIn2", button)));
        Assert.ThrowsExactly<ArgumentException>(() => form.AddControl("RDForms.CommandButton", "cmdIn3", new ComponentInstance(ComponentClasses.Frame, "Other")));

        Assert.AreEqual(3, form.Controls.Count);
        Assert.AreEqual(0, raised);
    }

    [TestMethod]
    public void RemovingAContainer_RemovesWhatItHolds_AndTheOtherTabIndexesCloseUp()
    {
        var form = Form();
        var frame = form.AddControl("RDForms.Frame", "fraNew");
        var inFrame = form.AddControl("RDForms.CommandButton", "cmdInNew", frame);
        var button = form.AddControl("RDForms.CommandButton", "cmdNew");
        var text = form.AddControl("RDForms.TextBox", "txtNew");
        var events = new List<string>();
        form.ControlRemoved += (_, e) => events.Add($"{e.Control.Name} from {e.Container.Name}");

        form.RemoveControl("FRANEW");

        Expect(["Frame1", "Command1", "cmdNew", "txtNew"], Names(form));
        Assert.IsNull(frame.Container);
        Assert.AreSame(frame, inFrame.Container);
        Assert.IsFalse(form.IsAddedAtRunTime(inFrame));
        Assert.AreEqual(((short)0, (short)1, (short)2, (short)3),
            (form.Find("Command1")!.Get(P.TabIndex), form.Find("Frame1")!.Get(P.TabIndex), button.Get(P.TabIndex), text.Get(P.TabIndex)));
        Expect(["fraNew from Form1"], events);
    }

    [TestMethod]
    public void OnlyAControlAdded_IsRemoved_AndOnceOnly()
    {
        var form = Form();
        var button = form.AddControl("RDForms.CommandButton", "cmdNew");
        form.RemoveControl(button);

        Assert.AreEqual(730, Error(() => form.RemoveControl("cmdNew")));
        Assert.AreEqual(730, Error(() => form.RemoveControl(button)));
        Assert.AreEqual(730, Error(() => form.RemoveControl("nothere")));
        Assert.AreEqual(729, Error(() => form.RemoveControl("Command1")));
        Assert.AreEqual(729, Error(() => form.RemoveControl(form.Controls[0])));
        Expect(["Frame1", "Command1"], Names(form));
    }

    [TestMethod]
    public void AContainerAddedThatHoldsAControlOfTheFormsFile_Is740_AndNothingIsRemoved()
    {
        var form = Form();
        var frame = form.AddControl("RDForms.Frame", "fraX");
        var command = form.Find("Command1")!;
        command.SetContainer(frame);

        var error = Assert.ThrowsExactly<FormErrorException>(() => form.RemoveControl(frame)).Error;

        Assert.AreEqual(740, error.Number);
        StringAssert.Contains(error.Description, "'Command1' was not added with Controls.Add.");
        Assert.AreSame(form.Root, frame.Container);
        Assert.AreSame(frame, command.Container);
        Assert.IsTrue(form.IsAddedAtRunTime(frame));
    }

    [TestMethod]
    public void ANameRemoved_CanBeAddedAgain()
    {
        var form = Form();
        form.RemoveControl(form.AddControl("RDForms.CommandButton", "cmdNew"));

        var again = form.AddControl("RDForms.CommandButton", "cmdNew");

        Assert.IsTrue(form.IsAddedAtRunTime(again));
        Assert.AreEqual((short)2, again.Get(P.TabIndex));
    }

    private static void Expect(string[] expected, IEnumerable<string> actual) =>
        CollectionAssert.AreEqual(expected, actual.ToArray(), $"expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
}

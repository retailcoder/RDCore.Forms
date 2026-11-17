using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// A program's Set Control.Container, as measured with VB6, native and p-code agreeing, on a form (twips) that holds Frame1 at
// 600, 600 (Picture2 in it, Check1 in Picture2), Picture1 at 3600, 600 in pixels, Command1 at 200, 300, Text1, Label1, Shape1, Line1,
// Timer1 and the menu mnuFile; a second form holds Frame2.
[TestClass]
public sealed class ContainerTests
{
    private static FormDefinition Form()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        var frame = Place(new ComponentInstance(ComponentClasses.Frame, "Frame1"), root, 600, 600, 0);
        var picture2 = Place(new ComponentInstance(ComponentClasses.PictureBox, "Picture2"), frame, 100, 240, 1);
        Place(new ComponentInstance(ComponentClasses.CheckBox, "Check1"), picture2, 50, 50, 2);
        Place(new ComponentInstance(ComponentClasses.PictureBox, "Picture1").Set(P.ScaleMode, ScaleMode.Pixel), root, 3600, 600, 4);
        Place(new ComponentInstance(ComponentClasses.CommandButton, "Command1"), root, 200, 300, 5);
        Place(new ComponentInstance(ComponentClasses.TextBox, "Text1"), root, 200, 3000, 6);
        Place(new ComponentInstance(ComponentClasses.Label, "Label1"), root, 200, 3500, 7);
        new ComponentInstance(ComponentClasses.Shape, "Shape1").Set(P.Left, 2000 / 15d).Set(P.Top, 3500 / 15d).SetContainer(root);
        new ComponentInstance(ComponentClasses.Line, "Line1").Set(P.X1, 3000d).Set(P.Y1, 3500d).Set(P.X2, 4000d).Set(P.Y2, 3900d).SetContainer(root);
        new ComponentInstance(ComponentClasses.Timer, "Timer1").SetContainer(root);
        new ComponentInstance(ComponentClasses.Menu, "mnuFile").Set(P.Caption, "File").SetContainer(root);
        return new FormDefinition(root);
    }

    // A control at a place in twips, as a form's file gives it; the model holds pixels.
    private static ComponentInstance Place(ComponentInstance control, ComponentInstance container, double left, double top, short tabIndex)
    {
        control.Set(P.Left, left / 15).Set(P.Top, top / 15).Set(P.TabIndex, tabIndex).SetContainer(container);
        return control;
    }

    private static ComponentInstance C(FormDefinition form, string name) => form.Find(name)!;

    // Left and Top as a VB6 program reads them.
    private static (double Left, double Top) Code(ComponentInstance control) =>
        (Math.Round(CodeUnits.ToCode(control, P.Left, control.Get(P.Left), CodeUnit.VB6), 5),
         Math.Round(CodeUnits.ToCode(control, P.Top, control.Get(P.Top), CodeUnit.VB6), 5));

    private static int Error(Action act) => Assert.ThrowsExactly<FormErrorException>(act).Error.Number;

    [TestMethod]
    public void AControlPlacedInAFrame_KeepsItsPlace_ItsVisibleAndEveryTabIndex_AndIsLastThere()
    {
        var form = Form();
        var events = new List<string>();
        form.ContainerChanged += (_, e) => events.Add($"{e.Control.Name}: {e.Previous.Name} to {e.Container.Name}");
        var button = C(form, "Command1");

        form.SetContainer(button, C(form, "Frame1"));

        Assert.AreSame(C(form, "Frame1"), button.Container);
        Assert.AreSame(button, C(form, "Frame1").Contents[^1]);
        Assert.IsFalse(form.Root.Contents.Contains(button));
        Assert.AreEqual((200d, 300d), Code(button));
        Assert.IsTrue(button.Get(P.Visible));
        CollectionAssert.AreEqual(new short[] { 0, 1, 2, 4, 5, 6, 7 },
            new[] { "Frame1", "Picture2", "Check1", "Picture1", "Command1", "Text1", "Label1" }.Select(n => C(form, n).Get(P.TabIndex)).ToArray());
        CollectionAssert.AreEqual(new[] { "Command1: Form1 to Frame1" }, events);

        form.SetContainer(button, form.Root);

        Assert.AreSame(form.Root, button.Container);
        Assert.AreEqual((200d, 300d), Code(button));
    }

    [TestMethod]
    public void InAPixelPictureBox_LeftAndTopAreTheSamePlace_InPixels()
    {
        var form = Form();
        var button = C(form, "Command1");
        var picture = C(form, "Picture1");

        form.SetContainer(button, picture);
        var inPixels = Code(button);
        picture.Set(P.ScaleMode, ScaleMode.Twip);
        var inTwips = Code(button);
        picture.Set(P.ScaleMode, ScaleMode.Pixel);
        button.Set(P.Left, CodeUnits.FromCode(button, P.Left, 10, CodeUnit.VB6));
        form.SetContainer(button, form.Root);

        Assert.AreEqual((13.33333, 20d), inPixels);
        Assert.AreEqual((200d, 300d), inTwips);
        Assert.AreEqual((150d, 300d), Code(button));
    }

    [TestMethod]
    public void AContainerPlaced_TakesWhatItHolds()
    {
        var form = Form();

        form.SetContainer(C(form, "Picture2"), form.Root);
        var check = C(form, "Check1").Container;
        form.SetContainer(C(form, "Picture2"), C(form, "Frame1"));
        form.SetContainer(C(form, "Frame1"), C(form, "Picture1"));

        Assert.AreSame(C(form, "Picture2"), check);
        Assert.AreEqual((40d, 40d), Code(C(form, "Frame1")));
        Assert.AreSame(C(form, "Frame1"), C(form, "Picture2").Container);
        Assert.AreEqual((100d, 240d), Code(C(form, "Picture2")));
    }

    [TestMethod]
    public void AControlPlacedInTheContainerItIsIn_IsPlacedLastThere()
    {
        var form = Form();
        var events = 0;
        form.ContainerChanged += (_, _) => events++;
        var button = C(form, "Command1");
        form.SetContainer(button, C(form, "Frame1"));
        form.SetContainer(C(form, "Picture2"), C(form, "Frame1"));

        form.SetContainer(button, C(form, "Frame1"));

        Assert.AreSame(button, C(form, "Frame1").Contents[^1]);
        Assert.AreEqual(3, events);
    }

    [TestMethod]
    public void AContainerTheControlCannotBeIn_Is425_AndNothingChanges()
    {
        var form = Form();
        var other = new ComponentInstance(ComponentClasses.Form, "Form2");
        var frame2 = new ComponentInstance(ComponentClasses.Frame, "Frame2");
        frame2.SetContainer(other);
        _ = new FormDefinition(other);
        var events = 0;
        form.ContainerChanged += (_, _) => events++;
        var before = form.Components.Select(c => (c.Name, c.Container?.Name)).ToArray();

        var errors = new[]
        {
            Error(() => form.SetContainer(C(form, "Command1"), C(form, "Text1"))),
            Error(() => form.SetContainer(C(form, "Frame1"), C(form, "Picture2"))),
            Error(() => form.SetContainer(C(form, "Frame1"), C(form, "Check1"))),
            Error(() => form.SetContainer(C(form, "Frame1"), C(form, "Frame1"))),
            Error(() => form.SetContainer(C(form, "Command1"), frame2)),
            Error(() => form.SetContainer(C(form, "Command1"), other)),
            Error(() => form.SetContainer(C(form, "Command1"), null)),
            Error(() => form.SetContainer(C(form, "Command1"), C(form, "mnuFile"))),
        };

        CollectionAssert.AreEqual(Enumerable.Repeat(425, 8).ToArray(), errors);
        CollectionAssert.AreEqual(before, form.Components.Select(c => (c.Name, c.Container?.Name)).ToArray());
        Assert.AreEqual(0, events);
    }

    [TestMethod]
    public void EveryControlHasAContainer_ButATimer_AndNoMenuItemOrRootHasOne()
    {
        var form = Form();

        CollectionAssert.AreEqual(new[] { "Timer1" }, form.Controls.Where(c => c.Class.Kind == ComponentKind.Control && !c.Class.HasContainer)
            .Select(c => c.Name).ToArray());
        Assert.IsFalse(ComponentClasses.Menu.HasContainer);
        Assert.IsFalse(ComponentClasses.Form.HasContainer);
        Assert.ThrowsExactly<ArgumentException>(() => form.SetContainer(C(form, "Timer1"), C(form, "Frame1")));
        Assert.ThrowsExactly<ArgumentException>(() => form.SetContainer(C(form, "mnuFile"), C(form, "Frame1")));
    }

    [TestMethod]
    public void AWindowlessControlPlacedInAFrame_KeepsItsPlace_AndALineItsEnds()
    {
        var form = Form();

        form.SetContainer(C(form, "Label1"), C(form, "Frame1"));
        form.SetContainer(C(form, "Shape1"), C(form, "Frame1"));
        form.SetContainer(C(form, "Line1"), C(form, "Frame1"));

        Assert.AreEqual((200d, 3500d), Code(C(form, "Label1")));
        Assert.AreEqual((2000d, 3500d), Code(C(form, "Shape1")));
        var line = C(form, "Line1");
        Assert.AreSame(C(form, "Frame1"), line.Container);
        Assert.AreEqual((3000d, 3500d, 4000d, 3900d), (line.Get(P.X1), line.Get(P.Y1), line.Get(P.X2), line.Get(P.Y2)));
    }

    [TestMethod]
    public void ALinePlacedInAPixelPictureBox_HasItsEndsInPixels_AtTheSamePlace_AndInTwipsBackInTheForm()
    {
        var form = Form();
        var line = C(form, "Line1");
        static (double, double, double, double) Ends(ComponentInstance l) =>
            (Math.Round(l.Get(P.X1), 4), Math.Round(l.Get(P.Y1), 4), Math.Round(l.Get(P.X2), 4), Math.Round(l.Get(P.Y2), 4));

        form.SetContainer(line, C(form, "Picture1"));
        var inPixels = Ends(line);
        form.SetContainer(C(form, "Label1"), C(form, "Picture1"));
        form.SetContainer(line, form.Root);

        Assert.AreEqual((200d, 233.3333, 266.6667, 260d), inPixels);
        Assert.AreEqual((13.33333, 233.33333), Code(C(form, "Label1")));
        Assert.AreEqual((3000d, 3500d, 4000d, 3900d), Ends(line));
    }

    [TestMethod]
    public void AControlAddedAtRunTime_PlacedInAnotherContainer_CanStillBeRemoved()
    {
        var form = Form();
        var added = form.AddControl("RDForms.CommandButton", "cmdNew", C(form, "Frame1"));

        form.SetContainer(added, form.Root);
        form.RemoveControl(added);

        Assert.IsNull(form.Find("cmdNew"));
    }
}

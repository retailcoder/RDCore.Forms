using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class FormDefinitionCopyTests
{
    private static FormDefinition Form()
    {
        var root = new ComponentInstance(VBClasses.Form, "Form1").Set(P.Caption, "Hello");
        root.UnmodelledProperties.Add(new UnmodelledProperty("LinkTopic", "\"Form1\""));
        var frame = new ComponentInstance(VBClasses.Frame, "Frame1").Set(P.Left, 8d);
        frame.SetContainer(root);
        new ComponentInstance(VBClasses.OptionButton, "Option1", 0).Set(P.Caption, "A").SetContainer(frame);
        new ComponentInstance(VBClasses.OptionButton, "Option1", 1).SetContainer(frame);
        root.UnmodelledContents.Add(new UnmodelledComponent("MSComctlLib.ListView", "ListView1", 1, "Begin MSComctlLib.ListView ListView1\r\nEnd"));
        var form = new FormDefinition(root);
        form.ControlLibraries.Add(new ControlLibraryReference(Guid.Empty, "2.0", "MSCOMCTL.OCX"));
        return form;
    }

    private static string Describe(FormDefinition form) => string.Join(" | ", form.Components.Select(c =>
        $"{c.Class.TypeName} {c} in {c.Container?.Name ?? "-"}: " + string.Join(", ", c.SetValues.Select(v => $"{v.Key.Name}={v.Value}"))));

    [TestMethod]
    public void ACopy_HasTheSameComponents_InTheSameContainers_WithTheSameValues()
    {
        var form = Form();

        var copy = form.Copy();

        Assert.AreEqual(Describe(form), Describe(copy));
        Assert.AreEqual("LinkTopic", copy.Root.UnmodelledProperties.Single().Name);
        Assert.AreEqual("ListView1", copy.Root.UnmodelledContents.Single().Name);
        Assert.AreEqual("MSCOMCTL.OCX", copy.ControlLibraries.Single().FileName);
    }

    [TestMethod]
    public void ACopysComponents_AreItsOwn()
    {
        var form = Form();

        var copy = form.Copy();
        copy.Find("Option1", 0)!.Set(P.Caption, "B");
        copy.Root.UnmodelledProperties.Clear();

        Assert.IsFalse(form.Components.Intersect(copy.Components, ReferenceEqualityComparer.Instance).Any());
        Assert.AreEqual("A", form.Find("Option1", 0)!.Get(P.Caption));
        Assert.HasCount(1, form.Root.UnmodelledProperties);
    }

    [TestMethod]
    public void ACopyOfAControlAddedAtRunTime_IsOneTheCopyAddedAtRunTime()
    {
        var form = Form();
        form.AddControl(VBClasses.TextBox, "Text1");

        var copy = form.Copy();

        Assert.IsTrue(copy.IsAddedAtRunTime(copy.Find("Text1")!));
        Assert.IsFalse(copy.IsAddedAtRunTime(copy.Find("Frame1")!));
        copy.RemoveControl("Text1");
        Assert.IsNotNull(form.Find("Text1"));
    }

    [TestMethod]
    public void AClassOfPlacedUserControls_GivesTheUserControlItWasMadeFrom()
    {
        var userControl = new FormDefinition(new ComponentInstance(VBClasses.UserControl, "Gauge"));

        Assert.AreSame(userControl, UserControlClasses.DefinitionOf(UserControlClasses.Of(userControl, "Project1")));
        Assert.IsNull(UserControlClasses.DefinitionOf(VBClasses.TextBox));
    }
}

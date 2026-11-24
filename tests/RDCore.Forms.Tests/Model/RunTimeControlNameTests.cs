using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// A program's assignment of a control's Name on an RDForms form, as measured with Microsoft Forms 2.0 in Excel: a control of the form's file keeps its name
// (382), and one Controls.Add added takes any name, a name taken, one that is not a legal control name, or none.
[TestClass]
public sealed class RunTimeControlNameTests
{
    private static FormDefinition Form()
    {
        var root = new ComponentInstance(ComponentClasses.Form, "Form1");
        new ComponentInstance(ComponentClasses.TextBox, "TextBox1").Set(P.TabIndex, (short)0).SetContainer(root);
        new ComponentInstance(ComponentClasses.TextBox, "TextBox2").Set(P.TabIndex, (short)1).SetContainer(root);
        return new FormDefinition(root);
    }

    [TestMethod]
    [DataRow("Renamed")]
    [DataRow("TextBox1")]
    [DataRow("TextBox2")]
    [DataRow("1 bad")]
    public void AControlOfTheFormsFile_KeepsItsName_382(string name)
    {
        var form = Form();
        var control = form.Find("TextBox1")!;

        var error = Assert.ThrowsExactly<FormErrorException>(() => form.RenameControl(control, name)).Error;

        Assert.AreEqual((382, "Could not set the Name property. Can not set property at runtime."), (error.Number, error.Description));
        Assert.AreEqual("TextBox1", control.Name);
    }

    [TestMethod]
    public void AControlAdded_IsFoundByItsNewName_AndNotItsOld_AndIsRemovedByIt()
    {
        var form = Form();
        var added = form.AddControl("RDForms.TextBox", "Added");

        form.RenameControl(added, "Renamed");

        Assert.AreEqual("Renamed", added.Name);
        Assert.AreSame(added, form.Find("Renamed"));
        Assert.IsNull(form.Find("Added"));
        form.RemoveControl("Renamed");
        Assert.HasCount(2, form.Controls);
    }

    [TestMethod]
    [DataRow("TextBox1", DisplayName = "a name taken")]
    [DataRow("1 bad", DisplayName = "not a legal control name")]
    [DataRow("", DisplayName = "none")]
    [DataRow("added", DisplayName = "its own, in another case")]
    public void AControlAdded_TakesAnyName(string name)
    {
        var form = Form();
        var added = form.AddControl("RDForms.TextBox", "Added");

        form.RenameControl(added, name);

        Assert.AreEqual(name, added.Name);
        Assert.AreEqual("TextBox1", form.Controls.First().Name, "the file's control keeps its name");
    }

    [TestMethod]
    public void AControlOfAnotherForm_IsNotFound_730()
    {
        var form = Form();
        var other = Form().Find("TextBox1")!;

        Assert.AreEqual(730, Assert.ThrowsExactly<FormErrorException>(() => form.RenameControl(other, "Renamed")).Error.Number);
    }
}

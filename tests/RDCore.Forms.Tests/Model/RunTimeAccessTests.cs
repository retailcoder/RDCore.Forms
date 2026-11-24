using RDCore.Forms.Model;
using P = RDCore.Forms.Model.Properties;

namespace RDCore.Forms.Tests.Model;

// The properties a running program cannot set (382), or can neither read (393) nor set, as measured with VB6 by reading each scalar property of
// each class late bound and assigning it, then a different value, native and p-code agreeing. On an RDForms form, a program sets the ones
// Microsoft Forms 2.0's class has writable.
[TestClass]
public sealed class RunTimeAccessTests
{
    private static readonly string[] WritableOnAnRDFormsForm =
        ["ComboBox.Style", "ListBox.IntegralHeight", "TextBox.MultiLine", "TextBox.ScrollBars", "ListBox.MultiSelect"];

    private static readonly string[] ReadOnly =
    [
        "Form.StartUpPosition", "Form.ShowInTaskbar", "Form.MaxButton", "Form.MinButton", "MDIForm.StartUpPosition",
        "CommandButton.Style", "CheckBox.Style", "OptionButton.Style", "ComboBox.IntegralHeight", "FileListBox.MultiSelect",
        "Menu.WindowList", "Menu.Shortcut", "Menu.NegotiatePosition", "Timer.Left", "Timer.Top", "Data.Negotiate",
        "ImageList.Left", "ImageList.Top",
    ];

    private static readonly string[] NotAvailable =
        ["Menu.Shortcut", "Menu.NegotiatePosition", "Timer.Left", "Timer.Top", "Data.Negotiate", "ImageList.Left", "ImageList.Top"];

    private static IEnumerable<string> Where(Func<ComponentClass, PropertyDefinition, bool> flagged) =>
        ComponentClasses.All.SelectMany(c => c.Properties.Where(p => flagged(c, p)).Select(p => $"{c.ClassName}.{p.Name}")).Order();

    [TestMethod]
    public void TheReadOnlyAtRunTimeProperties_OnAVisualBasic6Form_AreTheMeasuredOnes()
        => CollectionAssert.AreEqual(ReadOnly.Concat(WritableOnAnRDFormsForm).Order().ToArray(),
            Where((c, p) => c.IsReadOnlyAtRunTimeOnAVisualBasic6Form(p)).ToArray());

    [TestMethod]
    public void TheReadOnlyAtRunTimeProperties_OnAnRDFormsForm_LeaveOutTheOnesMSFormsWrites()
        => CollectionAssert.AreEqual(ReadOnly.Order().ToArray(), Where((c, p) => c.IsReadOnlyAtRunTime(p)).ToArray());

    [TestMethod]
    public void AProgramsAssignment_OfATextBoxsMultiLine_IsRefusedOnAVisualBasic6Form_AndSetOnAnRDFormsForm()
    {
        var box = new ComponentInstance(ComponentClasses.TextBox, "Text1");

        Assert.AreEqual(FormError.SetNotSupportedAtRuntime,
            Assert.ThrowsExactly<FormErrorException>(() => RunTime.Assign(box, P.MultiLine, true)).Error);
        RunTime.Assign(box, P.MultiLine, true, onAVisualBasic6Form: false);
        Assert.IsTrue(box.Get(P.MultiLine));
        Assert.ThrowsExactly<FormErrorException>(() =>
            RunTime.Assign(new ComponentInstance(ComponentClasses.FileListBox, "File1"), P.MultiSelect, ListBoxMultiSelect.Simple, onAVisualBasic6Form: false));
    }

    [TestMethod]
    public void AClassMakingAPropertyWritableOnAnRDFormsForm_ThatIsNotReadOnly_IsRefused()
        => Assert.ThrowsExactly<ArgumentException>(() => new ComponentClass("Test.Thing", "Thing", ComponentKind.Control,
            [P.Enabled], [], writableAtRunTimeOnAnRDFormsForm: [P.Enabled]));

    [TestMethod]
    public void ThePropertiesNotAvailableAtRunTime_AreTheMeasuredOnes()
        => CollectionAssert.AreEqual(NotAvailable.Order().ToArray(), Where((c, p) => c.IsNotAvailableAtRunTime(p)).ToArray());

    [TestMethod]
    public void ATimersLeftAndTop_AreNotAvailable_AndTheSameDefinitionsElsewhereAre()
    {
        Assert.IsTrue(ComponentClasses.Timer.IsNotAvailableAtRunTime(P.Left));
        Assert.IsTrue(ComponentClasses.Timer.IsReadOnlyAtRunTime(P.Top));
        Assert.IsFalse(P.Left.NotAvailableAtRunTime);
        Assert.IsFalse(ComponentClasses.MDIForm.IsNotAvailableAtRunTime(P.Left));
        Assert.IsFalse(ComponentClasses.MDIForm.IsReadOnlyAtRunTime(P.Top));
    }

    [TestMethod]
    public void AClassMakingAPropertyItDoesNotHaveUnavailable_IsRefused()
        => Assert.ThrowsExactly<ArgumentException>(() => new ComponentClass("Test.Thing", "Thing", ComponentKind.Control,
            [P.Enabled], [], notAvailableAtRunTime: [P.Left]));
}

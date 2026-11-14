using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// The properties a running program cannot set (382), or can neither read (393) nor set, as measured with VB6 by reading each scalar property of
// each class late bound and assigning it, then a different value, native and p-code agreeing.
[TestClass]
public sealed class RunTimeAccessTests
{
    private static readonly string[] ReadOnly =
    [
        "Form.StartUpPosition", "Form.ShowInTaskbar", "Form.MaxButton", "Form.MinButton", "MDIForm.StartUpPosition",
        "CommandButton.Style", "CheckBox.Style", "OptionButton.Style", "ComboBox.Style",
        "TextBox.MultiLine", "TextBox.ScrollBars", "ListBox.MultiSelect", "FileListBox.MultiSelect",
        "Menu.WindowList", "Menu.Shortcut", "Timer.Left", "Timer.Top",
    ];

    private static readonly string[] NotAvailable = ["Menu.Shortcut", "Timer.Left", "Timer.Top"];

    private static IEnumerable<string> Where(Func<ComponentClass, PropertyDefinition, bool> flagged) =>
        VBClasses.All.SelectMany(c => c.Properties.Where(p => flagged(c, p)).Select(p => $"{c.ClassName}.{p.Name}")).Order();

    [TestMethod]
    public void TheReadOnlyAtRunTimeProperties_AreTheMeasuredOnes()
        => CollectionAssert.AreEqual(ReadOnly.Order().ToArray(), Where((c, p) => c.IsReadOnlyAtRunTime(p)).ToArray());

    [TestMethod]
    public void ThePropertiesNotAvailableAtRunTime_AreTheMeasuredOnes()
        => CollectionAssert.AreEqual(NotAvailable.Order().ToArray(), Where((c, p) => c.IsNotAvailableAtRunTime(p)).ToArray());

    [TestMethod]
    public void ATimersLeftAndTop_AreNotAvailable_AndTheSameDefinitionsElsewhereAre()
    {
        Assert.IsTrue(VBClasses.Timer.IsNotAvailableAtRunTime(P.Left));
        Assert.IsTrue(VBClasses.Timer.IsReadOnlyAtRunTime(P.Top));
        Assert.IsFalse(P.Left.NotAvailableAtRunTime);
        Assert.IsFalse(VBClasses.MDIForm.IsNotAvailableAtRunTime(P.Left));
        Assert.IsFalse(VBClasses.MDIForm.IsReadOnlyAtRunTime(P.Top));
    }

    [TestMethod]
    public void AClassMakingAPropertyItDoesNotHaveUnavailable_IsRefused()
        => Assert.ThrowsExactly<ArgumentException>(() => new ComponentClass("Test.Thing", "Thing", ComponentKind.Control,
            [P.Enabled], [], notAvailableAtRunTime: [P.Left]));
}

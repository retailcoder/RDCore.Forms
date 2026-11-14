using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;
using P = RDCore.Forms.Model.VB.VBProperties;

namespace RDCore.Forms.Tests.Model;

// A TextBox's MultiLine, ScrollBars, PasswordChar and MaxLength, as measured with VB6, native and p-code agreeing.
[TestClass]
public sealed class TextBoxPropertyTests
{
    private static ComponentInstance Box() => new(VBClasses.TextBox, "Text1");

    [TestMethod]
    [DataRow(0, true)]
    [DataRow(65535, true)]
    [DataRow(-1, false)]
    [DataRow(65536, false)]
    [DataRow(70000, false)]
    [DataRow(int.MaxValue, false)]
    public void MaxLength_HoldsZeroTo65535(int length, bool held)
    {
        var box = Box().Set(P.MaxLength, 7);

        var set = box.TrySet(P.MaxLength, length);

        Assert.AreEqual(held, set);
        Assert.AreEqual(held ? length : 7, box.Get(P.MaxLength));
    }

    [TestMethod]
    [DataRow("*", true)]
    [DataRow(" ", true)]
    [DataRow("", true)]
    [DataRow("ab", false)]
    public void PasswordChar_HoldsOneCharacterOrNone(string character, bool held)
    {
        var box = Box().Set(P.PasswordChar, "#");

        var set = box.TrySet(P.PasswordChar, character);

        Assert.AreEqual(held, set);
        Assert.AreEqual(held ? character : "#", box.Get(P.PasswordChar));
    }

    [TestMethod]
    public void ATextSet_IsCutToMaxLength_WithOneChange()
    {
        var box = Box().Set(P.MaxLength, 3);
        var changes = new List<object?>();
        box.PropertyChanged += (_, e) =>
        {
            if (ReferenceEquals(e.Property, P.Text))
                changes.Add(e.After);
        };

        box.Set(P.Text, "abcdef");
        var tried = box.TrySet(P.Text, "uvwxyz");

        Assert.IsTrue(tried);
        Assert.AreEqual("uvw", box.Get(P.Text));
        CollectionAssert.AreEqual(new object?[] { "abc", "uvw" }, changes);
    }

    [TestMethod]
    public void ALowerMaxLength_KeepsTheLongerText_UntilTheTextIsSetAgain()
    {
        var box = Box().Set(P.Text, "12345");

        box.Set(P.MaxLength, 2);
        var kept = box.Get(P.Text);
        box.Set(P.Text, box.Get(P.Text));

        Assert.AreEqual("12345", kept);
        Assert.AreEqual("12", box.Get(P.Text));
    }

    [TestMethod]
    public void ATextSet_IsWholeWithNoMaxLength_AndInAComboBox()
    {
        var box = Box().Set(P.Text, "abcdef");
        var combo = new ComponentInstance(VBClasses.ComboBox, "Combo1").Set(P.Text, "abcdef");

        Assert.AreEqual("abcdef", box.Get(P.Text));
        Assert.AreEqual("abcdef", combo.Get(P.Text));
    }

    [TestMethod]
    public void MultiLineAndScrollBars_AreReadOnlyAtRunTime_AndPasswordCharAndMaxLengthAreNot()
    {
        Assert.IsTrue(P.MultiLine.ReadOnlyAtRunTime);
        Assert.IsTrue(P.ScrollBars.ReadOnlyAtRunTime);
        Assert.IsFalse(P.PasswordChar.ReadOnlyAtRunTime);
        Assert.IsFalse(P.MaxLength.ReadOnlyAtRunTime);
        Assert.IsFalse(P.Text.ReadOnlyAtRunTime);
    }

    [TestMethod]
    public void ATextBox_HasTheFourProperties_WithVBsDefaults()
    {
        var box = Box();

        Assert.IsFalse(box.Get(P.MultiLine));
        Assert.AreEqual(ScrollBars.None, box.Get(P.ScrollBars));
        Assert.AreEqual("", box.Get(P.PasswordChar));
        Assert.AreEqual(0, box.Get(P.MaxLength));
    }
}

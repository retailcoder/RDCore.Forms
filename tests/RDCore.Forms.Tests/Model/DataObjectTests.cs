using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

// A DataObject's text, in the text format and in custom formats, and a ReturnEffect's value.
[TestClass]
public sealed class DataObjectTests
{
    [TestMethod]
    public void TextSetInAFormat_IsReadInThatFormatOnly()
    {
        var data = new DataObject();
        data.SetText("plain");
        data.SetText("<b>bold</b>", "Html");

        Assert.AreEqual("plain", data.GetText());
        Assert.AreEqual("<b>bold</b>", data.GetText("Html"));
        Assert.IsNull(data.GetText("Rtf"));
        Assert.IsTrue(data.GetFormat());
        Assert.IsTrue(data.GetFormat("Html"));
        Assert.IsFalse(data.GetFormat("html"));
        CollectionAssert.AreEqual(new string?[] { null, "Html" }, data.Formats.ToArray());
    }

    [TestMethod]
    public void SetText_ReplacesTheTextOfItsFormat_AndClear_TakesOutEveryFormat()
    {
        var data = new DataObject();
        data.SetText("Html", "Html");
        data.SetText("first");
        data.SetText("second");

        Assert.AreEqual("second", data.GetText());
        data.Clear();
        Assert.IsFalse(data.GetFormat());
        Assert.IsEmpty(data.Formats);
    }

    [TestMethod]
    public void AnEmptyFormatName_IsRefused()
    {
        var data = new DataObject();

        Assert.ThrowsExactly<ArgumentException>(() => data.SetText("x", ""));
        Assert.ThrowsExactly<ArgumentException>(() => data.GetFormat(""));
    }

    [TestMethod]
    public void AReturnEffect_HoldsTheEffectAHandlerSets()
    {
        var effect = new ReturnEffect(DropEffect.Copy);
        effect.Value = DropEffect.Move;

        Assert.AreEqual(DropEffect.Move, effect.Value);
        Assert.AreEqual(DropEffect.None, new ReturnEffect().Value);
    }
}

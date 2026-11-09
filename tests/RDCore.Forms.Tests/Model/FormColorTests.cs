using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class FormColorTests
{
    [TestMethod]
    [DataRow(0x00FF8040, false)]
    [DataRow(unchecked((int)0x8000000F), true)]
    [DataRow(unchecked((int)0x80000018), true)]
    public void TryFromOle_TakesAnRgbOrASystemColour(int ole, bool isSystem)
    {
        var taken = FormColor.TryFromOle(ole, out var color);

        Assert.IsTrue(taken);
        Assert.AreEqual(isSystem, color.IsSystem);
        Assert.AreEqual(ole, color.ToOle());
    }

    [TestMethod]
    [DataRow(unchecked((int)0x80000019))]
    [DataRow(unchecked((int)0x80000100))]
    [DataRow(0x01000000)]
    [DataRow(-1)]
    public void TryFromOle_RefusesALongThatIsNoColour(int ole)
    {
        Assert.IsFalse(FormColor.TryFromOle(ole, out _));
    }

    [TestMethod]
    public void AnRgbColour_KeepsItsComponents_InTheOrderVBStoresThem()
    {
        var color = FormColor.FromRgb(0x40, 0x80, 0xFF);

        Assert.AreEqual(0x00FF8040, color.ToOle());
        Assert.AreEqual((byte)0x40, color.Red);
        Assert.AreEqual((byte)0xFF, color.Blue);
        Assert.IsNull(color.SystemColor);
    }

    [TestMethod]
    public void ToString_IsTheLiteralADesignerFileHolds()
    {
        Assert.AreEqual("&H8000000F&", FormColor.FromSystem(SystemColor.ButtonFace).ToString());
        Assert.AreEqual("&H00FF8040&", FormColor.FromRgb(0x40, 0x80, 0xFF).ToString());
    }
}

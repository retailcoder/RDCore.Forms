using RDCore.Forms.Model;
using RDCore.Forms.Model.VB;

namespace RDCore.Forms.Tests.Model;

[TestClass]
public sealed class FormFontTests
{
    // As measured with VB6, native and p-code agreeing: the size a font reads back once it is set. A TrueType font's is the size asked
    // in whole pixels, rounded half up and at least 2; a bitmap font's is one of its own, which can be larger than the size asked.
    [TestMethod]
    [DataRow("MS Sans Serif", 8, 8.25)]
    [DataRow("MS Sans Serif", 0.1, 8.25)]
    [DataRow("MS Sans Serif", 10, 9.75)]
    [DataRow("ms sans serif", 14, 13.5)]
    [DataRow("MS Sans Serif", 16.5, 16.5)]
    [DataRow("MS Sans Serif", 50, 48.75)]
    [DataRow("MS Sans Serif", 58.5, 60)]
    [DataRow("MS Sans Serif", 84, 86.25)]
    [DataRow("MS Sans Serif", 107.25, 112.5)]
    [DataRow("MS Sans Serif", 108, 108)]
    [DataRow("MS Sans Serif", 120, 120)]
    [DataRow("MS Sans Serif", 300, 300)]
    [DataRow("MS Serif", 6, 6)]
    [DataRow("MS Serif", 20, 20.25)]
    [DataRow("Courier", 8, 9.75)]
    [DataRow("Courier", 28, 24)]
    [DataRow("Courier", 89.25, 93.75)]
    [DataRow("System", 16, 9.75)]
    [DataRow("System", 18, 19.5)]
    [DataRow("System", 120, 78)]
    [DataRow("Small Fonts", 8, 8.25)]
    [DataRow("Small Fonts", 48, 45)]
    [DataRow("Small Fonts", 115.5, 115.5)]
    [DataRow("Arial", 8, 8.25)]
    [DataRow("Arial", 8.5, 8.25)]
    [DataRow("Arial", 6.375, 6.75)]
    [DataRow("Arial", 0.5, 1.5)]
    [DataRow("Arial", 2, 2.25)]
    [DataRow("Times New Roman", 10, 9.75)]
    [DataRow("Tahoma", 11.625, 12)]
    public void AtDrawnSize_IsTheSizeVB6ReadsBack(string name, double asked, double expected)
    {
        Assert.AreEqual(expected, new FormFont(name, asked).AtDrawnSize().Size);
    }

    // As measured with VB6: a font holds the size it reads back, so MS Sans Serif set to 14 points holds 13.5, and named Arial then
    // is drawn from 13.5 points, at 13.5, where 14 points of Arial are drawn at 14.25.
    [TestMethod]
    public void AComponentsFont_HoldsTheSizeItIsDrawnAt_AndANewNameIsDrawnFromIt()
    {
        var label = new ComponentInstance(VBClasses.Label, "Label1").Set(VBProperties.Font, new FormFont("MS Sans Serif", 14));
        var held = label.Get(VBProperties.Font);
        label.Set(VBProperties.Font, held with { Name = "Arial" });

        Assert.AreEqual("13.5, 13.5", $"{held.Size}, {label.Get(VBProperties.Font).Size}");
    }
}

using RDCore.Forms.Model;
using RDCore.SDK.Runtime.Stdole;

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

    // A size no StdFont takes is drawn at the smallest size, or the largest: 32767 pixels.
    [TestMethod]
    [DataRow("MS Sans Serif", 0, 8.25)]
    [DataRow("Arial", -3, 1.5)]
    [DataRow("Arial", 30000, 24575.25)]
    [DataRow("Arial", 1e30, 24575.25)]
    [DataRow("Arial", double.NaN, 24575.25)]
    public void AtDrawnSize_OfASizeNoStdFontTakes_IsTheSmallestOrTheLargest(string name, double asked, double expected)
    {
        Assert.AreEqual(expected, new FormFont(name, asked).AtDrawnSize().Size);
    }

    // A font never named is drawn in a scalable face whatever name it reads: whole pixels, with 2 pixels drawn as 3. Named, MS Sans
    // Serif is drawn at its own sizes.
    [TestMethod]
    [DataRow(0.5, true, 8.25)]
    [DataRow(0.5, false, 1.5)]
    [DataRow(1.5, true, 8.25)]
    [DataRow(1.5, false, 2.25)]
    [DataRow(8, false, 8.25)]
    [DataRow(14, true, 13.5)]
    [DataRow(14, false, 14.25)]
    public void AtDrawnSize_OfAFontNeverNamed_IsTheScalableFacesSize(double asked, bool named, double expected)
    {
        Assert.AreEqual(expected, new FormFont("MS Sans Serif", asked, IsNamed: named).AtDrawnSize().Size);
    }

    // As VB6 does with a New StdFont whose Size alone is set: it reads MS Sans Serif, and is drawn at 2 pixels' size as 3.
    [TestMethod]
    public void AStdFontNeverNamed_IsAFontNeverNamed_AndMakesOneNeverNamed()
    {
        var std = new StdFontObject();
        Assert.AreEqual(StdFontFailure.None, std.TrySetSize(1.5m));

        var font = FormFont.FromStdFont(std);
        Assert.AreEqual(new FormFont("MS Sans Serif", 2.25, IsNamed: false), font);
        Assert.AreEqual(StdFontFailure.None, font.TryToStdFont(out var made));
        Assert.AreEqual("False 2.25", $"{made!.IsNamed} {made.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
        Assert.AreEqual(StdFontFailure.None, std.TrySetName("MS Sans Serif"));
        Assert.AreEqual(new FormFont("MS Sans Serif", 8.25), FormFont.FromStdFont(std));
    }

    // As in VB6: MS Sans Serif set to 14 points is drawn at 13.5, and named Arial then is drawn from the 14 points set, at 14.25, as
    // when it was never drawn. A draw that kept the 13.5 it drew would draw Arial at 13.5.
    [TestMethod]
    public void TheFontAStdFontHolds_IsAtTheSizeItIsDrawnAt_AndLeavesTheSizeANewNameSnapsFrom()
    {
        var std = new StdFontObject();
        Assert.AreEqual(StdFontFailure.None, std.TrySetName("MS Sans Serif"));
        Assert.AreEqual(StdFontFailure.None, std.TrySetSize(14m));

        var drawn = FormFont.FromStdFont(std);
        Assert.AreEqual(StdFontFailure.None, std.TrySetName("Arial"));

        Assert.AreEqual("13.5, 14.25", $"{drawn.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)}, " +
            $"{std.Size.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
    }

    [TestMethod]
    public void AFont_MakesTheStdFontThatHoldsIt_AndIsTheFontThatStdFontHolds()
    {
        var font = new FormFont("Arial", 13.5, FormFont.BoldWeight, Italic: true, Strikethrough: true, Charset: 2);

        Assert.AreEqual(StdFontFailure.None, font.TryToStdFont(out var std));

        Assert.AreEqual("Arial 700 True False True 2", $"{std!.Name} {std.Weight} {std.Italic} {std.Underline} {std.Strikethrough} {std.Charset}");
        Assert.AreEqual(13.5m, std.Size);
        Assert.AreEqual(font, FormFont.FromStdFont(std));
    }

    // A StdFont stores a weight as 400 or 700, and reads back the size it draws.
    [TestMethod]
    public void TheFontAStdFontHolds_HasItsStoredWeight_AndTheSizeItReadsBack()
    {
        Assert.AreEqual(StdFontFailure.None, new FormFont("MS Sans Serif", 8, 551, Underline: true).TryToStdFont(out var std));

        Assert.AreEqual(new FormFont("MS Sans Serif", 8.25, FormFont.BoldWeight, Underline: true), FormFont.FromStdFont(std!));
        Assert.AreEqual(8.25m, std!.Size);
    }

    [TestMethod]
    [DataRow("Abcdefghijklmnopqrstuvwxyz012345", 8.25, 400, 0, StdFontFailure.InvalidPropertyValue)]
    [DataRow("Arial", 0, 400, 0, StdFontFailure.InvalidPropertyValue)]
    [DataRow("Arial", double.NaN, 400, 0, StdFontFailure.InvalidPropertyValue)]
    [DataRow("Arial", 24576, 400, 0, StdFontFailure.Overflow)]
    [DataRow("Arial", 1e30, 400, 0, StdFontFailure.Overflow)]
    [DataRow("Arial", 8.25, 1001, 0, StdFontFailure.InvalidPropertyValue)]
    [DataRow("Arial", 8.25, 70000, 0, StdFontFailure.InvalidPropertyValue)]
    [DataRow("Arial", 8.25, 400, 256, StdFontFailure.InvalidPropertyValue)]
    [DataRow("Arial", 8.25, 400, -1, StdFontFailure.InvalidPropertyValue)]
    [DataRow("Abcdefghijklmnopqrstuvwxyz012345", 24576, 400, 0, StdFontFailure.InvalidPropertyValue)]
    public void AFontWithAValueAStdFontRefuses_MakesNone_AndSaysWhichFailure(string name, double size, int weight, int charset, StdFontFailure expected)
    {
        Assert.AreEqual(expected, new FormFont(name, size, weight, Charset: charset).TryToStdFont(out var std));
        Assert.IsNull(std);
    }

    [TestMethod]
    public void FromStdFont_OfNull_Throws()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => FormFont.FromStdFont(null!));
    }

    // As measured with VB6: a font holds the size it reads back, so MS Sans Serif set to 14 points holds 13.5, and named Arial then
    // is drawn from 13.5 points, at 13.5, where 14 points of Arial are drawn at 14.25.
    [TestMethod]
    public void AComponentsFont_HoldsTheSizeItIsDrawnAt_AndANewNameIsDrawnFromIt()
    {
        var label = new ComponentInstance(ComponentClasses.Label, "Label1").Set(Properties.Font, new FormFont("MS Sans Serif", 14));
        var held = label.Get(Properties.Font);
        label.Set(Properties.Font, held with { Name = "Arial" });

        Assert.AreEqual("13.5, 13.5", $"{held.Size}, {label.Get(Properties.Font).Size}");
    }
}

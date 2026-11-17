using RDCore.Forms.Model;

namespace RDCore.Forms.Tests.Model;

// A length from the model's DIP to twips and points, and back.
[TestClass]
public sealed class UnitsTests
{
    private static readonly double[] Lengths = [0, 1, 0.1, 7.5, -12.25, 1 / 3d, 123.456, 1e9, -1e-9];

    [TestMethod]
    public void ADip_Is15Twips_And0Point75Point()
    {
        Assert.AreEqual(15d, Units.ToTwips(1));
        Assert.AreEqual(0.75, Units.ToPoints(1));
        Assert.AreEqual(Units.TwipsPerDip, Units.ToTwips(1));
        Assert.AreEqual(Units.PointsPerDip, Units.ToPoints(1));
        Assert.AreEqual(ScaleModeExtensions.TwipsPerPixel, Units.ToTwips(1));
    }

    [TestMethod]
    public void AnInch_Is1440Twips_96Dips_And72Points()
    {
        Assert.AreEqual(96d, Units.FromTwips(1440));
        Assert.AreEqual(1440d, Units.ToTwips(96));
        Assert.AreEqual(72d, Units.ToPoints(96));
        Assert.AreEqual(96d, Units.FromPoints(72));
    }

    [TestMethod]
    [DataRow(300d, 20d, 400d)]
    [DataRow(0d, 0d, 0d)]
    [DataRow(7d, 7 / 15d, 7 / 0.75)]
    [DataRow(-150d, -10d, -200d)]
    public void ALegacyLength_ConvertsToDips(double legacy, double fromTwips, double fromPoints)
    {
        Assert.AreEqual(fromTwips, Units.FromTwips(legacy), 1e-12);
        Assert.AreEqual(fromPoints, Units.FromPoints(legacy), 1e-12);
    }

    [TestMethod]
    [DataRow(20d, 300d, 15d)]
    [DataRow(0d, 0d, 0d)]
    [DataRow(0.5, 7.5, 0.375)]
    [DataRow(-10d, -150d, -7.5)]
    public void ADipLength_ConvertsToLegacyUnits(double dips, double twips, double points)
    {
        Assert.AreEqual(twips, Units.ToTwips(dips));
        Assert.AreEqual(points, Units.ToPoints(dips));
    }

    [TestMethod]
    public void TwipsAndPoints_RoundTripThroughDips()
    {
        foreach (var length in Lengths)
        {
            var tolerance = Math.Abs(length) * 1e-15;
            Assert.AreEqual(length, Units.FromTwips(Units.ToTwips(length)), tolerance, $"dips {length} to twips");
            Assert.AreEqual(length, Units.ToTwips(Units.FromTwips(length)), tolerance, $"twips {length} to dips");
            Assert.AreEqual(length, Units.FromPoints(Units.ToPoints(length)), tolerance, $"dips {length} to points");
            Assert.AreEqual(length, Units.ToPoints(Units.FromPoints(length)), tolerance, $"points {length} to dips");
        }
    }

    [TestMethod]
    public void TwipsAndPoints_ConvertThroughDips()
    {
        foreach (var length in Lengths)
        {
            var tolerance = Math.Abs(length) * 1e-15;
            Assert.AreEqual(length / 20, Units.ToPoints(Units.FromTwips(length)), tolerance, $"twips {length} to points");
            Assert.AreEqual(length * 20, Units.ToTwips(Units.FromPoints(length)), tolerance * 20, $"points {length} to twips");
        }
    }
}

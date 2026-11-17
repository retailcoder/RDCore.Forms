namespace RDCore.Forms.Model;

/// <summary>
/// Conversions between the model's unit, the device-independent pixel (DIP, 1/96 inch), and the units of legacy forms code: twips
/// (VB6) and points (MSForms).
/// </summary>
/// <remarks>
/// A length converts by a constant: 1 DIP is 15 twips and 0.75 point. Lengths are Doubles, so a conversion keeps fractions.
/// </remarks>
public static class Units
{
    /// <summary>
    /// The twips in a DIP: 15, as a twip is 1/1440 inch.
    /// </summary>
    public const double TwipsPerDip = 15;

    /// <summary>
    /// The points in a DIP: 0.75, as a point is 1/72 inch.
    /// </summary>
    public const double PointsPerDip = 0.75;

    /// <summary>
    /// A length in twips, in DIP.
    /// </summary>
    /// <param name="twips">The length in twips.</param>
    /// <returns>The length in DIP: <paramref name="twips"/> over <see cref="TwipsPerDip"/>.</returns>
    public static double FromTwips(double twips) => twips / TwipsPerDip;

    /// <summary>
    /// A length in DIP, in twips.
    /// </summary>
    /// <param name="dips">The length in DIP.</param>
    /// <returns>The length in twips: <paramref name="dips"/> times <see cref="TwipsPerDip"/>.</returns>
    public static double ToTwips(double dips) => dips * TwipsPerDip;

    /// <summary>
    /// A length in points, in DIP.
    /// </summary>
    /// <param name="points">The length in points.</param>
    /// <returns>The length in DIP: <paramref name="points"/> over <see cref="PointsPerDip"/>.</returns>
    public static double FromPoints(double points) => points / PointsPerDip;

    /// <summary>
    /// A length in DIP, in points.
    /// </summary>
    /// <param name="dips">The length in DIP.</param>
    /// <returns>The length in points: <paramref name="dips"/> times <see cref="PointsPerDip"/>.</returns>
    public static double ToPoints(double dips) => dips * PointsPerDip;
}

using System.Globalization;

namespace RDCore.Forms.Porting;

// A legacy value as the port compares it, and as its messages write it.
internal static class LegacyValue
{
    // A number with no fraction as an Int64, a Boolean as VBA's -1 or 0, anything else as it is.
    public static object? Normalize(object? value) => value switch
    {
        bool b => b ? -1L : 0L,
        sbyte or byte or short or ushort or int or uint or long => System.Convert.ToInt64(value, CultureInfo.InvariantCulture),
        ulong u => u <= long.MaxValue ? (long)u : (object)u,
        float f => Whole(f),
        double d => Whole(d),
        decimal m => decimal.Truncate(m) == m && m >= long.MinValue && m <= long.MaxValue ? (long)m : (object)m,
        _ => value,
    };

    // The value as VBA source writes it.
    public static string Text(object? value) => value switch
    {
        null => "Nothing",
        DBNull => "Null",
        bool b => b ? "True" : "False",
        string s => "\"" + s.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    // 2^63, the first Double past Int64.MaxValue.
    private const double Int64Limit = 9223372036854775808.0;

    private static object Whole(double value) =>
        double.IsFinite(value) && Math.Truncate(value) == value && value >= -Int64Limit && value < Int64Limit ? (long)value : (object)value;
}

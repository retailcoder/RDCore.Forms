using System.Globalization;
using RDCore.Forms.Model;

namespace RDCore.Forms.Xaml;

// The text of a property's value in an RDForms document, and the value of a text.
internal static class FormXamlValues
{
    // The text of a value written as an attribute, or null for a value written as an element (a font, a list).
    public static string? Format(object? value) => value switch
    {
        string text => text,
        bool flag => flag ? "True" : "False",
        short or int => Convert.ToString(value, CultureInfo.InvariantCulture),
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        Enum member => Enum.IsDefined(member.GetType(), member)
            ? member.ToString()
            : Convert.ToInt64(member, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
        FormColor color => Format(color),
        byte[] bytes => Convert.ToBase64String(bytes),
        _ => null,
    };

    // A system colour by its name, an RGB colour as #RRGGBB.
    private static string Format(FormColor color) =>
        color.SystemColor is { } system
            ? system.ToString()
            : string.Create(CultureInfo.InvariantCulture, $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}");

    // The value of a text, for a property of the type given; false where the text is not one, or the type is not written as text.
    public static bool TryParse(Type type, string text, out object? value)
    {
        value = null;
        if (type == typeof(string))
            value = text;
        else if (type == typeof(bool) && bool.TryParse(text, out var flag))
            value = flag;
        else if (type == typeof(short) && short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var small))
            value = small;
        else if (type == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var whole))
            value = whole;
        else if (type == typeof(double) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            && double.IsFinite(number))
            value = number;
        else if (type.IsEnum)
            value = ParseEnum(type, text);
        else if (type == typeof(FormColor))
            value = ParseColor(text);
        else if (type == typeof(byte[]))
            value = ParsePicture(text);
        return value is not null;
    }

    // A member's name, without regard to case, or a number, which keeps a value the enum does not name.
    private static object? ParseEnum(Type type, string text)
    {
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
            return Enum.ToObject(type, number);
        var name = Enum.GetNames(type).FirstOrDefault(n => string.Equals(n, text.Trim(), StringComparison.OrdinalIgnoreCase));
        return name is null ? null : Enum.Parse(type, name);
    }

    private static object? ParseColor(string text)
    {
        text = text.Trim();
        if (text.Length == 7 && text[0] == '#' && uint.TryParse(text.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var rgb))
            return FormColor.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        var name = Enum.GetNames<SystemColor>().FirstOrDefault(n => string.Equals(n, text, StringComparison.OrdinalIgnoreCase));
        return name is null ? null : FormColor.FromSystem(Enum.Parse<SystemColor>(name));
    }

    // A picture's file bytes, in base64. A text that would decode to more than a picture can hold is refused before it is decoded.
    public static byte[]? ParsePicture(string text)
    {
        if ((long)text.Length * 3 / 4 > FormPictures.MaxBytes + 3)
            return null;
        var bytes = new byte[text.Length * 3 / 4 + 3];
        return Convert.TryFromBase64String(text, bytes, out var written) ? bytes[..written] : null;
    }
}

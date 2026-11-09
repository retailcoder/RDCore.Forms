// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Globalization;

namespace RDCore.Forms.Import.VB6;

// A property's value as a designer file writes it.
internal abstract record ValueLiteral
{
    // "Caption", with a quote inside written twice.
    public sealed record Text(string Value) : ValueLiteral;

    // 120, -1, 9.75, 32767&, &H8000000F&, True. A hexadecimal literal is a Long, so &H8000000F& is negative.
    public sealed record Number(double Value, bool IsIntegral) : ValueLiteral;

    // "Form1.frx":0A00 or $"Form1.frx":0000: a record of the companion binary file, at a hexadecimal offset.
    public sealed record Citation(string FileName, int Offset, bool IsDollar) : ValueLiteral;

    // Anything else, such as a menu's Shortcut, ^C.
    public sealed record Word(string Value) : ValueLiteral;

    public static ValueLiteral Parse(string value)
    {
        var dollar = value.StartsWith("$\"", StringComparison.Ordinal);
        if (dollar || value.StartsWith('"'))
        {
            return ParseQuoted(value, dollar ? 1 : 0, dollar) ?? new Word(value);
        }

        var comment = value.IndexOf('\'');
        var text = (comment < 0 ? value : value[..comment]).Trim();

        if (text.StartsWith("&H", StringComparison.OrdinalIgnoreCase))
        {
            var digits = text[2..].TrimEnd('&');
            return digits.Length is > 0 and <= 8
                && uint.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var hex)
                    ? new Number(unchecked((int)hex), true)
                    : new Word(value);
        }

        if (text.Equals("True", StringComparison.OrdinalIgnoreCase))
        {
            return new Number(-1, true);
        }

        if (text.Equals("False", StringComparison.OrdinalIgnoreCase))
        {
            return new Number(0, true);
        }

        // a designer file writes no NaN and no infinity, and a number past a Double's range, such as 1E400, is a word.
        var number = text.EndsWith('&') ? text[..^1] : text;
        return double.TryParse(number, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed)
            ? new Number(parsed, Math.Floor(parsed) == parsed)
            : new Word(value);
    }

    private static ValueLiteral? ParseQuoted(string value, int open, bool dollar)
    {
        var builder = new System.Text.StringBuilder();
        var at = open + 1;
        while (true)
        {
            var quote = value.IndexOf('"', at);
            if (quote < 0)
            {
                return null;
            }

            builder.Append(value, at, quote - at);
            if (quote + 1 < value.Length && value[quote + 1] == '"')
            {
                builder.Append('"');
                at = quote + 2;
                continue;
            }

            at = quote + 1;
            break;
        }

        var rest = value[at..].Trim();
        if (rest.StartsWith(':'))
        {
            var digits = rest[1..].Trim();
            return int.TryParse(digits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var offset)
                ? new Citation(builder.ToString(), offset, dollar)
                : null;
        }

        return dollar || rest.Length > 0 && !rest.StartsWith('\'') ? null : new Text(builder.ToString());
    }
}

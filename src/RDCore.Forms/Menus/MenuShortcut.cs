using System.Text;

namespace RDCore.Forms.Menus;

/// <summary>
/// The modifier keys of a <see cref="MenuShortcut"/>, with the values of VB's <c>Shift</c> argument.
/// </summary>
[Flags]
public enum ShortcutModifiers
{
    /// <summary>
    /// No modifier.
    /// </summary>
    None = 0,

    /// <summary>
    /// Shift.
    /// </summary>
    Shift = 1,

    /// <summary>
    /// Ctrl.
    /// </summary>
    Ctrl = 2,

    /// <summary>
    /// Alt.
    /// </summary>
    Alt = 4,
}

/// <summary>
/// The key that chooses a menu item from anywhere on its form: a key, by its VB <c>KeyCode</c>, with modifiers.
/// </summary>
/// <remarks>
/// <para>
/// Its text is VB's: a modifier prefix (<c>^</c> for Ctrl, <c>+</c> for Shift, <c>%</c> for Alt) then a letter, a digit, or a key
/// name in braces, as in <c>^Q</c>, <c>{F2}</c> and <c>+^{DEL}</c>. The key names are the ones VB's <c>SendKeys</c> statement
/// takes: <c>BACKSPACE</c> (or <c>BS</c> or <c>BKSP</c>), <c>BREAK</c>, <c>CAPSLOCK</c>, <c>DELETE</c> (or <c>DEL</c>),
/// <c>DOWN</c>, <c>END</c>, <c>ENTER</c>, <c>ESC</c>, <c>HELP</c>, <c>HOME</c>, <c>INSERT</c> (or <c>INS</c>), <c>LEFT</c>,
/// <c>NUMLOCK</c>, <c>PGDN</c>, <c>PGUP</c>, <c>PRTSC</c>, <c>RIGHT</c>, <c>SCROLLLOCK</c>, <c>TAB</c>, <c>UP</c>, and <c>F1</c> to
/// <c>F16</c>, in any case.
/// </para>
/// <para>
/// A VB6 menu's shortcut is one of the keys its Menu Editor lists. Any key of the names above, with any modifiers, is an RD-VBA
/// addition.
/// </para>
/// </remarks>
/// <param name="KeyCode">The key's VB <c>KeyCode</c>, such as 81 for Q and 113 for F2.</param>
/// <param name="Modifiers">The modifier keys held with it.</param>
public readonly record struct MenuShortcut(short KeyCode, ShortcutModifiers Modifiers = ShortcutModifiers.None)
{
    // The braced names, the first of each key's being the one its text is written with.
    private static readonly (string Name, short KeyCode)[] Names =
    [
        ("BKSP", 8), ("BACKSPACE", 8), ("BS", 8), ("BREAK", 3), ("CAPSLOCK", 20), ("DEL", 46), ("DELETE", 46), ("DOWN", 40),
        ("END", 35), ("ENTER", 13), ("ESC", 27), ("HELP", 47), ("HOME", 36), ("INSERT", 45), ("INS", 45), ("LEFT", 37),
        ("NUMLOCK", 144), ("PGDN", 34), ("PGUP", 33), ("PRTSC", 44), ("RIGHT", 39), ("SCROLLLOCK", 145), ("TAB", 9), ("UP", 38),
        .. Enumerable.Range(1, 16).Select(n => ($"F{n}", (short)(111 + n))),
    ];

    /// <summary>
    /// Reads a shortcut written as VB writes it, such as <c>^Q</c> or <c>{F2}</c>.
    /// </summary>
    /// <param name="text">The shortcut's text.</param>
    /// <param name="shortcut">The shortcut read, or the default where the text is not one.</param>
    /// <returns>Whether the text is a shortcut.</returns>
    public static bool TryParse(string? text, out MenuShortcut shortcut)
    {
        shortcut = default;
        if (string.IsNullOrEmpty(text))
            return false;
        var modifiers = ShortcutModifiers.None;
        var i = 0;
        for (; i < text.Length; i++)
        {
            var modifier = text[i] switch
            {
                '^' => ShortcutModifiers.Ctrl,
                '+' => ShortcutModifiers.Shift,
                '%' => ShortcutModifiers.Alt,
                _ => ShortcutModifiers.None,
            };
            if (modifier == ShortcutModifiers.None)
                break;
            modifiers |= modifier;
        }
        var key = text[i..];
        short code;
        if (key.Length == 1 && char.IsAsciiLetter(key[0]))
            code = (short)char.ToUpperInvariant(key[0]);
        else if (key.Length == 1 && char.IsAsciiDigit(key[0]))
            code = (short)key[0];
        else if (key.Length > 2 && key[0] == '{' && key[^1] == '}'
                 && Array.FindIndex(Names, n => string.Equals(n.Name, key[1..^1], StringComparison.OrdinalIgnoreCase)) is >= 0 and var at)
            code = Names[at].KeyCode;
        else
            return false;
        shortcut = new MenuShortcut(code, modifiers);
        return true;
    }

    /// <summary>
    /// The shortcut as VB writes it: its modifiers in the order <c>^</c>, <c>+</c>, <c>%</c>, then its key.
    /// </summary>
    /// <returns>The shortcut's text, or an empty string for a key that has no letter, digit or name.</returns>
    public override string ToString()
    {
        var code = KeyCode;
        string? key = code switch
        {
            >= (short)'A' and <= (short)'Z' or >= (short)'0' and <= (short)'9' => ((char)code).ToString(),
            _ => Array.FindIndex(Names, n => n.KeyCode == code) is >= 0 and var at ? $"{{{Names[at].Name}}}" : null,
        };
        if (key is null)
            return "";
        var text = new StringBuilder();
        if (Modifiers.HasFlag(ShortcutModifiers.Ctrl))
            text.Append('^');
        if (Modifiers.HasFlag(ShortcutModifiers.Shift))
            text.Append('+');
        if (Modifiers.HasFlag(ShortcutModifiers.Alt))
            text.Append('%');
        return text.Append(key).ToString();
    }
}

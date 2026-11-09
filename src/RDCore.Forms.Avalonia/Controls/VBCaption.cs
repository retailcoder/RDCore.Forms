// Derived from HexIDE (https://github.com/hexide-io/HexIDE), commit ae17f1db.
// Copyright (c) 2024 Bartosz Korczyński. Copyright (c) 2026 The HexIDE Authors. MIT License; see NOTICE.md.

using System.Text;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace RDCore.Forms.Avalonia.Controls;

/// <summary>
/// A caption's access key, read as VB reads it, and the control that draws the caption.
/// </summary>
/// <remarks>
/// <para>
/// VB shows <c>&amp;&amp;</c> as one <c>&amp;</c>. A single <c>&amp;</c> is hidden and marks the character after it; a trailing
/// <c>&amp;</c>, with nothing to mark, is not shown; and <c>_</c> is an ordinary character. Where a caption marks more than one
/// character, a control underlines the last, and a menu answers only to the first.
/// </para>
/// <para>
/// Avalonia's <see cref="AccessText"/> marks its key with <c>_</c> and draws <c>__</c> as one underscore, but takes its key from the
/// first underscore in its text, doubled or not. So only a caption whose key comes before every underscore it shows is drawn as an
/// <see cref="AccessText"/>. Any other is drawn as plain text, with no key, so that it does not answer to a key VB does not give it.
/// </para>
/// </remarks>
public static class VBCaption
{
    /// <summary>
    /// Which marked character is the access key, where a caption marks more than one.
    /// </summary>
    public enum KeyChoice
    {
        /// <summary>
        /// The last, which a control underlines.
        /// </summary>
        Last,

        /// <summary>
        /// The first, the only one a menu answers to.
        /// </summary>
        First,
    }

    /// <summary>
    /// A caption as shown, and its access key.
    /// </summary>
    /// <param name="Shown">The caption as shown, its <c>&amp;</c> markers taken out.</param>
    /// <param name="AccessKey">The index in <paramref name="Shown"/> of the access key, or <see langword="null"/> where it has none.</param>
    public readonly record struct Parsed(string Shown, int? AccessKey)
    {
        /// <summary>
        /// Whether an <see cref="AccessText"/> can draw the caption: it has a key, and no underscore is shown before the key.
        /// </summary>
        public bool FitsAccessText => AccessKey is { } key && Shown.IndexOf('_', 0, key) < 0;

        /// <summary>
        /// The caption spelled for an <see cref="AccessText"/>: every underscore doubled, and one in front of the key. Where it does
        /// not <see cref="FitsAccessText"/>, the doubled underscores alone.
        /// </summary>
        public string ForAccessText
        {
            get
            {
                var drawn = Shown.Replace("_", "__");
                if (!FitsAccessText)
                    return drawn;
                var key = AccessKey!.Value;
                return drawn[..key] + "_" + drawn[key..];
            }
        }
    }

    /// <summary>
    /// Reads a caption as VB reads it.
    /// </summary>
    /// <param name="caption">The caption.</param>
    /// <param name="choice">Which marked character is the key, where it marks more than one.</param>
    /// <returns>
    /// The caption as shown, and its key: <c>E&amp;xit</c> shows <c>Exit</c> with <c>x</c> its key, <c>Tom &amp;&amp; Jerry</c> shows
    /// <c>Tom &amp; Jerry</c> with no key, and <c>Trail&amp;</c> shows <c>Trail</c>.
    /// </returns>
    public static Parsed Parse(string caption, KeyChoice choice = KeyChoice.Last)
    {
        ArgumentNullException.ThrowIfNull(caption);
        if (!caption.Contains('&'))
            return new Parsed(caption, null);

        var shown = new StringBuilder(caption.Length);
        int? key = null;
        for (var i = 0; i < caption.Length; i++)
        {
            if (caption[i] != '&')
            {
                shown.Append(caption[i]);
            }
            else if (i + 1 < caption.Length && caption[i + 1] == '&')
            {
                shown.Append('&');
                i++;
            }
            else if (i + 1 < caption.Length && (choice == KeyChoice.Last || key is null))
            {
                // The marked character is appended on the next pass.
                key = shown.Length;
            }
        }
        return new Parsed(shown.ToString(), key);
    }

    /// <summary>
    /// A caption as shown.
    /// </summary>
    /// <param name="caption">The caption.</param>
    /// <returns>The caption with its <c>&amp;</c> markers read: <c>E&amp;xit</c> is <c>Exit</c>.</returns>
    public static string AsShown(string caption) => Parse(caption).Shown;

    /// <summary>
    /// The control that draws a caption: an <see cref="AccessText"/> where the caption <see cref="Parsed.FitsAccessText"/>, a
    /// <see cref="TextBlock"/> where it does not.
    /// </summary>
    /// <param name="caption">The caption.</param>
    /// <param name="choice">Which marked character is the key, where it marks more than one.</param>
    /// <returns>The control.</returns>
    public static TextBlock Present(string caption, KeyChoice choice = KeyChoice.Last)
    {
        var parsed = Parse(caption, choice);
        return parsed.FitsAccessText ? new AccessText { Text = parsed.ForAccessText } : new TextBlock { Text = parsed.Shown };
    }

    // A content control's template for a string caption. Not recycled: the child's type depends on the caption.
    internal static readonly FuncDataTemplate Template =
        new FuncDataTemplate<object>((data, _) => data is string caption ? Present(caption) : null, false);

    // Names a control for automation by its caption as shown, or clears the name where the caption shows nothing.
    internal static void NameFor(Control control, string caption)
    {
        if (AsShown(caption) is { Length: > 0 } shown)
            control.SetValue(AutomationProperties.NameProperty, shown);
        else
            control.ClearValue(AutomationProperties.NameProperty);
    }

    // A content control that draws its caption with its access key.
    internal static void ShowIn(ContentControl control, string caption)
    {
        control.ContentTemplate = Template;
        control.Content = caption;
        NameFor(control, caption);
    }

    // The same, for a headered control's header.
    internal static void ShowIn(HeaderedContentControl control, string caption)
    {
        control.HeaderTemplate = Template;
        control.Header = caption;
        NameFor(control, caption);
    }
}

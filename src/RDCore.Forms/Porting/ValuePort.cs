namespace RDCore.Forms.Porting;

/// <summary>
/// How a legacy member's values convert to its RDForms member's values: TextAlign's 1, 2 and 3 to Alignment's 0, 2 and 1, or a VB6
/// CheckBox's 0, 1 and 2 to <c>False</c>, <c>True</c> and <c>Null</c>.
/// </summary>
/// <remarks>
/// <para>
/// A legacy value is a number or a Boolean, as a literal in the legacy code gives it. Numbers compare by value, whatever their type,
/// so the Integer 1, the Long 1 and the Double 1.0 are the same value. A Boolean compares as VBA's number for it: <c>True</c> is
/// -1 and <c>False</c> is 0.
/// </para>
/// <para>
/// An RDForms value is an Int32 for a number, a Boolean, or <see cref="DBNull.Value"/> for VBA's <c>Null</c>.
/// </para>
/// </remarks>
public sealed record ValuePort
{
    private readonly Dictionary<object, object> byLegacyValue;

    /// <summary>
    /// Creates the conversion of a member's values.
    /// </summary>
    /// <param name="values">Each documented legacy value, and the RDForms value it converts to.</param>
    /// <param name="constants">
    /// Each legacy constant of the member's values, and the RDForms constant or literal the code port writes for it, as VBA source
    /// text.
    /// </param>
    /// <param name="grayedComparesWithIsNull">
    /// Whether the legacy value that converts to <c>Null</c> is compared with <c>IsNull(...)</c> in RDForms code.
    /// </param>
    /// <exception cref="ArgumentException">Two of the legacy values of <paramref name="values"/> are the same value.</exception>
    public ValuePort(IReadOnlyDictionary<object, object> values, IReadOnlyDictionary<string, string> constants, bool grayedComparesWithIsNull = false)
    {
        Values = values;
        Constants = new Dictionary<string, string>(constants, StringComparer.OrdinalIgnoreCase);
        GrayedComparesWithIsNull = grayedComparesWithIsNull;
        byLegacyValue = values.ToDictionary(v => LegacyValue.Normalize(v.Key) ?? v.Key, v => v.Value);
    }

    /// <summary>
    /// Each documented legacy value, and the RDForms value it converts to.
    /// </summary>
    public IReadOnlyDictionary<object, object> Values { get; }

    /// <summary>
    /// Each legacy constant of the member's values, such as <c>vbChecked</c> or <c>fmTextAlignCenter</c>, and the RDForms constant
    /// or literal the code port writes for it, as VBA source text, such as <c>True</c> or <c>2</c>. The names compare without
    /// regard to case.
    /// </summary>
    public IReadOnlyDictionary<string, string> Constants { get; }

    /// <summary>
    /// Whether a comparison with the legacy value that converts to <c>Null</c> becomes an <c>IsNull(...)</c> test, as
    /// <c>chk.Value = vbGrayed</c> becomes <c>IsNull(chk.Value)</c>. An assignment of that value becomes an assignment of
    /// <c>Null</c>.
    /// </summary>
    public bool GrayedComparesWithIsNull { get; }

    /// <summary>
    /// Converts a legacy value to its RDForms value.
    /// </summary>
    /// <remarks>
    /// The conversion is total: every value of <see cref="Values"/> converts, and any other input, <see langword="null"/>, a string
    /// or an object included, gives a result that is not convertible. It never throws.
    /// </remarks>
    /// <param name="legacy">The legacy value, such as a literal of the legacy code.</param>
    /// <returns>The converted value, or a result whose <see cref="ValueConversion.Diagnostic"/> the code port reports.</returns>
    public ValueConversion Convert(object? legacy) =>
        LegacyValue.Normalize(legacy) is { } key && byLegacyValue.TryGetValue(key, out var value)
            ? new ValueConversion(true, legacy, value)
            : new ValueConversion(false, legacy, null);
}

/// <summary>
/// The result of <see cref="ValuePort.Convert"/>: the RDForms value of a legacy value, or the fact that it has none.
/// </summary>
/// <param name="IsConvertible">Whether the legacy value is one the member's values convert from.</param>
/// <param name="Legacy">The legacy value that was converted.</param>
/// <param name="Value">The RDForms value, or <see langword="null"/> when <paramref name="IsConvertible"/> is <see langword="false"/>.</param>
public readonly record struct ValueConversion(bool IsConvertible, object? Legacy, object? Value)
{
    /// <summary>
    /// What the code port reports at a value that is not convertible: <see cref="PortDiagnosticKind.ValueNotConvertible"/>, naming
    /// the legacy value. It is <see langword="null"/> when the value converts.
    /// </summary>
    public PortDiagnostic? Diagnostic => IsConvertible ? null : PortDiagnostic.ValueNotConvertible(Legacy);
}

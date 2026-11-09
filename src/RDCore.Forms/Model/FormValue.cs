namespace RDCore.Forms.Model;

/// <summary>
/// The kinds of value a <see cref="FormValue"/> holds: one per VB <c>Variant</c> subtype.
/// </summary>
public enum FormValueKind
{
    /// <summary>
    /// No value: an uninitialized <c>Variant</c>.
    /// </summary>
    Empty,
    /// <summary>
    /// <c>Null</c>: a value known to be missing.
    /// </summary>
    Null,
    /// <summary>
    /// A Boolean, held as <see cref="bool"/>.
    /// </summary>
    Boolean,
    /// <summary>
    /// A Byte, held as <see cref="byte"/>.
    /// </summary>
    Byte,
    /// <summary>
    /// An Integer, held as <see cref="short"/>.
    /// </summary>
    Integer,
    /// <summary>
    /// A Long, held as <see cref="int"/>.
    /// </summary>
    Long,
    /// <summary>
    /// A LongLong, held as <see cref="long"/>.
    /// </summary>
    LongLong,
    /// <summary>
    /// A Single, held as <see cref="float"/>.
    /// </summary>
    Single,
    /// <summary>
    /// A Double, held as <see cref="double"/>.
    /// </summary>
    Double,
    /// <summary>
    /// A Currency, held as <see cref="decimal"/>.
    /// </summary>
    Currency,
    /// <summary>
    /// A Decimal, held as <see cref="decimal"/>.
    /// </summary>
    Decimal,
    /// <summary>
    /// A Date, held as <see cref="DateTime"/>.
    /// </summary>
    Date,
    /// <summary>
    /// A String, held as <see cref="string"/>.
    /// </summary>
    String,
    /// <summary>
    /// An error value, as <c>CVErr</c> makes one, held as its number, an <see cref="int"/>.
    /// </summary>
    Error,
    /// <summary>
    /// An object: a control, a form, or an object a control's member returns, such as a font. <c>Nothing</c> is an object value
    /// that holds <see langword="null"/>.
    /// </summary>
    Object,
    /// <summary>
    /// An array, held as a <see cref="FormArray"/>.
    /// </summary>
    Array,
}

/// <summary>
/// A value passed to or returned by a member of a form or a control, or passed to an event's handler: a kind and the CLR value of
/// that kind.
/// </summary>
/// <remarks>
/// A form value converts nothing. A control receives a value of the type its member declares, because the code that runs the form
/// let-coerces it before the call, a late-bound call included. A control that cannot take a value it is given answers with a
/// <see cref="FormError"/>.
/// </remarks>
public readonly struct FormValue : IEquatable<FormValue>
{
    private readonly object? value;

    private FormValue(FormValueKind kind, object? value)
    {
        Kind = kind;
        this.value = value;
    }

    /// <summary>
    /// The kind of value held.
    /// </summary>
    public FormValueKind Kind { get; }

    /// <summary>
    /// The CLR value held: <see langword="null"/> for <see cref="FormValueKind.Empty"/>, <see cref="FormValueKind.Null"/> and
    /// <c>Nothing</c>; otherwise a value of the CLR type its <see cref="Kind"/> names.
    /// </summary>
    public object? ClrValue => value;

    /// <summary>
    /// The uninitialized value.
    /// </summary>
    public static FormValue Empty => default;

    /// <summary>
    /// The value <c>Null</c>.
    /// </summary>
    public static FormValue Null { get; } = new(FormValueKind.Null, null);

    /// <summary>
    /// The object value <c>Nothing</c>.
    /// </summary>
    public static FormValue Nothing { get; } = new(FormValueKind.Object, null);

    /// <summary>
    /// A Boolean.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromBoolean(bool value) => new(FormValueKind.Boolean, value);

    /// <summary>
    /// A Byte.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromByte(byte value) => new(FormValueKind.Byte, value);

    /// <summary>
    /// An Integer.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromInteger(short value) => new(FormValueKind.Integer, value);

    /// <summary>
    /// A Long.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromLong(int value) => new(FormValueKind.Long, value);

    /// <summary>
    /// A LongLong.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromLongLong(long value) => new(FormValueKind.LongLong, value);

    /// <summary>
    /// A Single.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromSingle(float value) => new(FormValueKind.Single, value);

    /// <summary>
    /// A Double.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromDouble(double value) => new(FormValueKind.Double, value);

    /// <summary>
    /// A Currency.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromCurrency(decimal value) => new(FormValueKind.Currency, value);

    /// <summary>
    /// A Decimal.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromDecimal(decimal value) => new(FormValueKind.Decimal, value);

    /// <summary>
    /// A Date.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromDate(DateTime value) => new(FormValueKind.Date, value);

    /// <summary>
    /// A String.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The form value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static FormValue FromString(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(FormValueKind.String, value);
    }

    /// <summary>
    /// An error value.
    /// </summary>
    /// <param name="number">The error's number.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromError(int number) => new(FormValueKind.Error, number);

    /// <summary>
    /// An object, or <c>Nothing</c> for <see langword="null"/>.
    /// </summary>
    /// <param name="target">The object.</param>
    /// <returns>The form value.</returns>
    public static FormValue FromObject(object? target) => new(FormValueKind.Object, target);

    /// <summary>
    /// An array.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <returns>The form value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    public static FormValue FromArray(FormArray array)
    {
        ArgumentNullException.ThrowIfNull(array);
        return new(FormValueKind.Array, array);
    }

    /// <summary>
    /// The Boolean held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Boolean.</exception>
    public bool AsBoolean() => As<bool>(FormValueKind.Boolean);

    /// <summary>
    /// The Byte held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Byte.</exception>
    public byte AsByte() => As<byte>(FormValueKind.Byte);

    /// <summary>
    /// The Integer held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not an Integer.</exception>
    public short AsInteger() => As<short>(FormValueKind.Integer);

    /// <summary>
    /// The Long held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Long.</exception>
    public int AsLong() => As<int>(FormValueKind.Long);

    /// <summary>
    /// The LongLong held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a LongLong.</exception>
    public long AsLongLong() => As<long>(FormValueKind.LongLong);

    /// <summary>
    /// The Single held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Single.</exception>
    public float AsSingle() => As<float>(FormValueKind.Single);

    /// <summary>
    /// The Double held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Double.</exception>
    public double AsDouble() => As<double>(FormValueKind.Double);

    /// <summary>
    /// The Currency held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Currency.</exception>
    public decimal AsCurrency() => As<decimal>(FormValueKind.Currency);

    /// <summary>
    /// The Decimal held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Decimal.</exception>
    public decimal AsDecimal() => As<decimal>(FormValueKind.Decimal);

    /// <summary>
    /// The Date held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a Date.</exception>
    public DateTime AsDate() => As<DateTime>(FormValueKind.Date);

    /// <summary>
    /// The String held.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The value is not a String.</exception>
    public string AsString() => As<string>(FormValueKind.String);

    /// <summary>
    /// The number of the error value held.
    /// </summary>
    /// <returns>The error's number.</returns>
    /// <exception cref="InvalidOperationException">The value is not an error value.</exception>
    public int AsError() => As<int>(FormValueKind.Error);

    /// <summary>
    /// The object held, or <see langword="null"/> for <c>Nothing</c>.
    /// </summary>
    /// <returns>The object.</returns>
    /// <exception cref="InvalidOperationException">The value is not an object.</exception>
    public object? AsObject()
    {
        Require(FormValueKind.Object);
        return value;
    }

    /// <summary>
    /// The array held.
    /// </summary>
    /// <returns>The array.</returns>
    /// <exception cref="InvalidOperationException">The value is not an array.</exception>
    public FormArray AsArray() => As<FormArray>(FormValueKind.Array);

    /// <summary>
    /// Whether two values are of the same kind and hold equal CLR values. Two objects are equal when they are the same object, and two
    /// arrays when they are the same array.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    public bool Equals(FormValue other) =>
        Kind == other.Kind && (Kind is FormValueKind.Object or FormValueKind.Array
            ? ReferenceEquals(value, other.value)
            : Equals(value, other.value));

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is FormValue other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, value is null ? 0 : Kind is FormValueKind.Object or FormValueKind.Array
        ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value)
        : value.GetHashCode());

    /// <summary>
    /// Whether two values are equal, as <see cref="Equals(FormValue)"/> compares them.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when the values are equal.</returns>
    public static bool operator ==(FormValue left, FormValue right) => left.Equals(right);

    /// <summary>
    /// Whether two values differ, as <see cref="Equals(FormValue)"/> compares them.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when the values differ.</returns>
    public static bool operator !=(FormValue left, FormValue right) => !left.Equals(right);

    /// <summary>
    /// The value's kind and CLR value, for diagnostics.
    /// </summary>
    /// <returns>A description of the value, such as <c>Long 42</c>.</returns>
    public override string ToString() => Kind switch
    {
        FormValueKind.Empty or FormValueKind.Null => Kind.ToString(),
        FormValueKind.Object when value is null => "Nothing",
        _ => $"{Kind} {value}",
    };

    private T As<T>(FormValueKind kind)
    {
        Require(kind);
        return (T)value!;
    }

    private void Require(FormValueKind kind)
    {
        if (Kind != kind)
            throw new InvalidOperationException($"The value is {Kind}, not {kind}.");
    }
}

/// <summary>
/// A one-dimensional array of form values, with its lower bound.
/// </summary>
public sealed class FormArray
{
    /// <summary>
    /// Creates an array.
    /// </summary>
    /// <param name="elementKind">
    /// The declared kind of the array's elements: <see cref="FormValueKind.Empty"/> for an array of <c>Variant</c>, whose elements are
    /// of any kind.
    /// </param>
    /// <param name="lowerBound">The index of the first element.</param>
    /// <param name="elements">The elements, in index order.</param>
    /// <exception cref="ArgumentException">
    /// An element is not of <paramref name="elementKind"/>, for an array whose elements have a declared kind.
    /// </exception>
    public FormArray(FormValueKind elementKind, int lowerBound, IReadOnlyList<FormValue> elements)
    {
        if (elementKind != FormValueKind.Empty && elements.Any(e => e.Kind != elementKind))
            throw new ArgumentException($"An element is not {elementKind}.", nameof(elements));
        ElementKind = elementKind;
        LowerBound = lowerBound;
        Elements = [.. elements];
    }

    /// <summary>
    /// The declared kind of the array's elements, or <see cref="FormValueKind.Empty"/> for an array of <c>Variant</c>.
    /// </summary>
    public FormValueKind ElementKind { get; }

    /// <summary>
    /// The index of the first element.
    /// </summary>
    public int LowerBound { get; }

    /// <summary>
    /// The index of the last element: one less than <see cref="LowerBound"/> for an empty array.
    /// </summary>
    public int UpperBound => LowerBound + Elements.Count - 1;

    /// <summary>
    /// The elements, in index order.
    /// </summary>
    public IReadOnlyList<FormValue> Elements { get; }

    /// <summary>
    /// The element at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The element's index, from <see cref="LowerBound"/> to <see cref="UpperBound"/>.</param>
    /// <returns>The element.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the array's bounds.</exception>
    public FormValue this[int index] =>
        index < LowerBound || index > UpperBound
            ? throw new ArgumentOutOfRangeException(nameof(index), index, null)
            : Elements[index - LowerBound];
}

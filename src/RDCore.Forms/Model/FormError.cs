using System.Globalization;
using System.Resources;

namespace RDCore.Forms.Model;

/// <summary>
/// A run-time error a form or a control answers with: VB's error number, and its standard description.
/// </summary>
/// <remarks>
/// <para>
/// A control refuses a value, a member or a call with the error VB raises for it, such as 380 for a value outside a property's range.
/// The code that runs the form raises it in the program, where <c>Err.Number</c> reads <see cref="Number"/>.
/// </para>
/// <para>
/// Each static property of this type makes its error with the description in the current UI culture, such as "Invalid property value"
/// in English and "Valeur de propriété incorrecte" in French.
/// </para>
/// </remarks>
/// <param name="Number">The error's number, as <c>Err.Number</c> reads it.</param>
/// <param name="Description">The error's description, as <c>Err.Description</c> reads it.</param>
public readonly record struct FormError(int Number, string Description)
{
    private static readonly ResourceManager Descriptions = new("RDCore.Forms.Model.FormErrors", typeof(FormError).Assembly);

    /// <summary>
    /// Error 5, "Invalid procedure call or argument".
    /// </summary>
    public static FormError InvalidProcedureCall => Standard(5);

    /// <summary>
    /// Error 6, "Overflow".
    /// </summary>
    public static FormError Overflow => Standard(6);

    /// <summary>
    /// Error 13, "Type mismatch".
    /// </summary>
    public static FormError TypeMismatch => Standard(13);

    /// <summary>
    /// Error 53, "File not found".
    /// </summary>
    public static FormError FileNotFound => Standard(53);

    /// <summary>
    /// Error 68, "Device unavailable".
    /// </summary>
    public static FormError DeviceUnavailable => Standard(68);

    /// <summary>
    /// Error 76, "Path not found".
    /// </summary>
    public static FormError PathNotFound => Standard(76);

    /// <summary>
    /// Error 94, "Invalid use of Null".
    /// </summary>
    public static FormError InvalidUseOfNull => Standard(94);

    /// <summary>
    /// Error 343, "Object not an array".
    /// </summary>
    public static FormError ObjectNotAnArray => Standard(343);

    /// <summary>
    /// Error 360, "Object already loaded".
    /// </summary>
    public static FormError ObjectAlreadyLoaded => Standard(360);

    /// <summary>
    /// Error 361, "Can't load or unload this object".
    /// </summary>
    public static FormError CantLoadOrUnloadThisObject => Standard(361);

    /// <summary>
    /// Error 362, "Can't unload controls created at design time".
    /// </summary>
    public static FormError CantUnloadControlsCreatedAtDesignTime => Standard(362);

    /// <summary>
    /// Error 380, "Invalid property value".
    /// </summary>
    public static FormError InvalidPropertyValue => Standard(380);

    /// <summary>
    /// Error 381, "Invalid property array index".
    /// </summary>
    public static FormError InvalidPropertyArrayIndex => Standard(381);

    /// <summary>
    /// Error 382, "Set not supported at runtime": a property a program cannot assign while it runs.
    /// </summary>
    public static FormError SetNotSupportedAtRuntime => Standard(382);

    /// <summary>
    /// Error 383, "Set not supported (read-only property)".
    /// </summary>
    public static FormError SetNotSupportedReadOnly => Standard(383);

    /// <summary>
    /// Error 387, "'<paramref name="property"/>' property can't be set on this control": a value the property takes, refused on
    /// this component, such as <c>Checked</c> on a top-level menu.
    /// </summary>
    /// <param name="property">The property's name.</param>
    /// <returns>The error.</returns>
    public static FormError PropertyCantBeSetOnThisControl(string property)
    {
        var error = Standard(387);
        return error with { Description = string.Format(CultureInfo.CurrentUICulture, error.Description, property) };
    }

    /// <summary>
    /// Error 395, "Cannot use separator bar as menu name for this control": a menu's <c>Caption</c> set to a hyphen.
    /// </summary>
    public static FormError CantUseSeparatorBarAsMenuName => Standard(395);

    /// <summary>
    /// Error 393, "Get not supported at runtime": a property a program cannot read while it runs.
    /// </summary>
    public static FormError GetNotSupportedAtRuntime => Standard(393);

    /// <summary>
    /// Error 425, "Invalid object use".
    /// </summary>
    public static FormError InvalidObjectUse => Standard(425);

    /// <summary>
    /// Error 438, "Object doesn't support this property or method".
    /// </summary>
    public static FormError ObjectDoesntSupportPropertyOrMethod => Standard(438);

    /// <summary>
    /// Error 448, "Named argument not found".
    /// </summary>
    public static FormError NamedArgumentNotFound => Standard(448);

    /// <summary>
    /// Error 449, "Argument not optional".
    /// </summary>
    public static FormError ArgumentNotOptional => Standard(449);

    /// <summary>
    /// Error 450, "Wrong number of arguments or invalid property assignment".
    /// </summary>
    public static FormError WrongNumberOfArguments => Standard(450);

    /// <summary>
    /// Error 461, "Method or data member not found".
    /// </summary>
    public static FormError MethodOrDataMemberNotFound => Standard(461);

    /// <summary>
    /// Error 481, "Invalid picture".
    /// </summary>
    public static FormError InvalidPicture => Standard(481);

    /// <summary>
    /// The error's number and description, such as <c>380: Invalid property value</c>.
    /// </summary>
    /// <returns>The error's number and description.</returns>
    public override string ToString() => $"{Number}: {Description}";

    private static FormError Standard(int number) =>
        new(number, Descriptions.GetString($"Error{number}", CultureInfo.CurrentUICulture) ?? $"Error {number}");
}

/// <summary>
/// The exception a form or a control throws to answer with a <see cref="FormError"/>.
/// </summary>
/// <remarks>
/// The exception's message is the error's description.
/// </remarks>
/// <param name="error">The error.</param>
public sealed class FormErrorException(FormError error) : Exception(error.Description)
{
    /// <summary>
    /// The error the form or the control answers with.
    /// </summary>
    public FormError Error { get; } = error;
}

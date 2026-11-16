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
    /// Error 91, "Object variable or With block variable not set": such as a Data control's <c>UpdateRecord</c> with no recordset.
    /// </summary>
    public static FormError ObjectVariableNotSet => Standard(91);

    /// <summary>
    /// Error 94, "Invalid use of Null".
    /// </summary>
    public static FormError InvalidUseOfNull => Standard(94);

    /// <summary>
    /// Error 340, "Control array element '<paramref name="index"/>' doesn't exist": an element of a control array that is not
    /// loaded.
    /// </summary>
    /// <param name="index">The element's index.</param>
    /// <returns>The error.</returns>
    public static FormError ControlArrayElementDoesntExist(int index) => Formatted(340, index.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Error 341, "Invalid object array index": a negative index given to <c>Load</c>.
    /// </summary>
    public static FormError InvalidObjectArrayIndex => Standard(341);

    /// <summary>
    /// Error 341, "Invalid control array index": a negative index given to <c>Load</c> for an element of a control array of controls
    /// other than menus, as VB6 words it for them.
    /// </summary>
    public static FormError InvalidControlArrayIndex =>
        new(341, Descriptions.GetString("Error341Control", CultureInfo.CurrentUICulture) ?? "Error 341");

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
    /// Error 383, "'<paramref name="name"/>' property is read-only": the description VB6 gives an assignment to a control's read-only
    /// property, such as a ListBox's <c>ListCount</c>.
    /// </summary>
    /// <param name="name">The property's name.</param>
    /// <returns>The error.</returns>
    public static FormError PropertyIsReadOnly(string name) =>
        new(383, string.Format(CultureInfo.CurrentUICulture, Descriptions.GetString("Error383Named", CultureInfo.CurrentUICulture) ?? "'{0}' property is read-only", name));

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
    /// Error 711, "Invalid class string. Looking for object with ProgID: <paramref name="progId"/>": a class that
    /// <c>Controls.Add</c> does not know.
    /// </summary>
    /// <param name="progId">The class's name, as the program gave it.</param>
    /// <returns>The error.</returns>
    public static FormError InvalidClassString(string progId) => Formatted(711, progId);

    /// <summary>
    /// Error 720, "'<paramref name="className"/>' is not a valid control type": a class <c>Controls.Add</c> makes no control of,
    /// such as a menu or a form.
    /// </summary>
    /// <param name="className">The class's name, such as <c>Menu</c>.</param>
    /// <returns>The error.</returns>
    public static FormError NotAValidControlType(string className) => Formatted(720, className);

    /// <summary>
    /// Error 721, "'<paramref name="className"/>' is not a valid control container": a control that holds no controls, given to
    /// <c>Controls.Add</c> as a container.
    /// </summary>
    /// <param name="className">The would-be container's class name, such as <c>CommandButton</c>.</param>
    /// <returns>The error.</returns>
    public static FormError NotAValidControlContainer(string className) => Formatted(721, className);

    /// <summary>
    /// Error 727, "There is already a control with the name '<paramref name="name"/>'".
    /// </summary>
    /// <param name="name">The name, as the program gave it.</param>
    /// <returns>The error.</returns>
    public static FormError ControlNameTaken(string name) => Formatted(727, name);

    /// <summary>
    /// Error 728, "'<paramref name="name"/>' is not a legal control name".
    /// </summary>
    /// <param name="name">The name, as the program gave it.</param>
    /// <returns>The error.</returns>
    public static FormError NotALegalControlName(string name) => Formatted(728, name);

    /// <summary>
    /// Error 729, "Controls.Remove can only remove controls added with Controls.Add".
    /// </summary>
    public static FormError CanOnlyRemoveAddedControls => Standard(729);

    /// <summary>
    /// Error 730, "Control '<paramref name="name"/>' not found".
    /// </summary>
    /// <param name="name">The name, as the program gave it.</param>
    /// <returns>The error.</returns>
    public static FormError ControlNotFound(string name) => Formatted(730, name);

    /// <summary>
    /// Error 735, "The control is no longer usable because it has been removed from the controls collection".
    /// </summary>
    public static FormError ControlRemoved => Standard(735);

    /// <summary>
    /// Error 740, "Controls.Remove can only remove container controls that contain controls added with Controls.Add. '<paramref
    /// name="name"/>' was not added with Controls.Add.": a container that holds a control placed at design time.
    /// </summary>
    /// <param name="name">The name of the control placed at design time.</param>
    /// <returns>The error.</returns>
    public static FormError ContainerHoldsADesignTimeControl(string name) => Formatted(740, name);

    /// <summary>
    /// Error -2147467262 (&amp;H80004002), "System Error &amp;H80004002 (-2147467262). No such interface supported": the object is not of
    /// the kind asked for, such as the name of a control array given to <c>Controls.Remove</c>.
    /// </summary>
    public static FormError NoSuchInterface => Standard(unchecked((int)0x80004002));

    /// <summary>
    /// The error's number and description, such as <c>380: Invalid property value</c>.
    /// </summary>
    /// <returns>The error's number and description.</returns>
    public override string ToString() => $"{Number}: {Description}";

    private static FormError Standard(int number) =>
        new(number, Descriptions.GetString($"Error{number}", CultureInfo.CurrentUICulture) ?? $"Error {number}");

    private static FormError Formatted(int number, string argument)
    {
        var error = Standard(number);
        return error with { Description = string.Format(CultureInfo.CurrentUICulture, error.Description, argument) };
    }
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

using System.Globalization;
using RDCore.Forms.Model;

namespace RDCore.Forms.Avalonia.Rendering;

// The arguments of a method call, checked as VB6 checks them and coerced as a VB6 control coerces a Variant it is given.
internal static class MethodArguments
{
    // One argument per parameter, null where one is left out. Too many is 450 and a required one left out 449. One left out before one
    // given is 448, as VB6's Move 600, , 4800 is.
    internal static FormValue?[] Of(MethodDefinition method, IReadOnlyList<FormValue?> arguments)
    {
        if (arguments.Count > method.Parameters.Count)
            throw new FormErrorException(FormError.WrongNumberOfArguments);
        var given = new FormValue?[method.Parameters.Count];
        for (var i = 0; i < arguments.Count; i++)
            given[i] = arguments[i];
        for (var i = 0; i < method.RequiredCount; i++)
        {
            if (given[i] is null)
                throw new FormErrorException(FormError.ArgumentNotOptional);
        }
        var last = Array.FindLastIndex(given, argument => argument is not null);
        for (var i = 0; i < last; i++)
        {
            if (given[i] is null)
                throw new FormErrorException(FormError.NamedArgumentNotFound);
        }
        return given;
    }

    // A Single: Null is 94, a value with no number 13, and one past a Single's range 6.
    internal static float Single(FormValue value)
    {
        var number = Number(value, FormError.InvalidUseOfNull);
        if (Math.Abs(number) > float.MaxValue)
            throw new FormErrorException(FormError.Overflow);
        return (float)number;
    }

    // An Integer, rounded half to even: a value with no number 13, Null what the caller says, and one past an Integer's range 6.
    internal static short Integer(FormValue value, FormError ofNull)
    {
        var number = Math.Round(Number(value, ofNull));
        if (number is < short.MinValue or > short.MaxValue)
            throw new FormErrorException(FormError.Overflow);
        return (short)number;
    }

    // A Long, rounded half to even: a value with no number 13, Null 94, and one past a Long's range 6.
    internal static int Long(FormValue value)
    {
        var number = Math.Round(Number(value, FormError.InvalidUseOfNull));
        if (number is < int.MinValue or > int.MaxValue)
            throw new FormErrorException(FormError.Overflow);
        return (int)number;
    }

    // A Boolean: True for a number other than 0, and for the string "True" without regard to case, False for "False" (MS-VBAL §5.5.1.2.2).
    // Null is 94, and another value with no number 13.
    internal static bool Boolean(FormValue value) => value switch
    {
        { Kind: FormValueKind.Boolean } => value.AsBoolean(),
        { Kind: FormValueKind.String } when bool.TryParse(value.AsString().Trim(), out var truth) => truth,
        _ => Number(value, FormError.InvalidUseOfNull) != 0,
    };

    // A String: Empty is "", Null 94. The caller gives a String parameter a String (see RDFormsFormWindow.Invoke); another value is 13.
    internal static string String(FormValue value) => value.Kind switch
    {
        FormValueKind.String => value.AsString(),
        FormValueKind.Empty => "",
        FormValueKind.Null => throw new FormErrorException(FormError.InvalidUseOfNull),
        _ => throw new FormErrorException(FormError.TypeMismatch),
    };

    private static double Number(FormValue value, FormError ofNull) => value.Kind switch
    {
        FormValueKind.Empty => 0,
        FormValueKind.Null => throw new FormErrorException(ofNull),
        FormValueKind.Boolean => value.AsBoolean() ? -1 : 0,
        FormValueKind.Byte or FormValueKind.Integer or FormValueKind.Long or FormValueKind.LongLong or FormValueKind.Single
            or FormValueKind.Double or FormValueKind.Currency or FormValueKind.Decimal =>
            Convert.ToDouble(value.ClrValue, CultureInfo.InvariantCulture),
        FormValueKind.Date => value.AsDate().ToOADate(),
        FormValueKind.String when double.TryParse(value.AsString().Trim(), NumberStyles.Float | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture, out var parsed) => parsed,
        _ => throw new FormErrorException(FormError.TypeMismatch),
    };
}

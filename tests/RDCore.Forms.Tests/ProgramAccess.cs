using RDCore.Forms.Model;

namespace RDCore.Forms.Tests;

// A program's read and assignment of a component's property, as the runtime's host makes them: one the component refuses is VB's 380.
internal static class ProgramAccess
{
    internal static void Let(this ComponentInstance component, PropertyDefinition property, object value)
    {
        if (!component.TrySet(property, value))
            throw new FormErrorException(FormError.InvalidPropertyValue);
    }

    internal static object Read(this ComponentInstance component, PropertyDefinition property) =>
        component.TryGet(property, out var value) ? value! : throw new FormErrorException(FormError.InvalidPropertyValue);
}
